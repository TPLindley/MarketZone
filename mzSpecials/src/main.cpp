#include <gtkmm.h>
#include <pangomm.h>
#include <librsvg/rsvg.h>
#include <vector>
#include <string>
#include <mutex>
#include <thread>
#include <fstream>
#include <filesystem>
#include <functional>
#include "json.hpp"
#include "httplib.h"

using json = nlohmann::json;

// --- API port ---
static const int API_PORT = 8765;

// --- Scroll speed ---
static const double SCROLL_SPEED = 40.0;

// -----------------------------------------------------------------------
// Special entry
// -----------------------------------------------------------------------
struct Special {
    std::string text;
    std::string color_hex; // e.g. "#FFFFFF"

    Special() : text(""), color_hex("#FFFFFF") {}
    explicit Special(const std::string& t, const std::string& c = "#FFFFFF")
        : text(t), color_hex(c) {}

    Gdk::RGBA rgba() const {
        Gdk::RGBA c;
        if (!c.set(color_hex))
            c.set_rgba(1.0, 1.0, 1.0, 1.0);
        return c;
    }
};

// -----------------------------------------------------------------------
// Persistence
// -----------------------------------------------------------------------
static std::string data_file_path()
{
    std::string dir = std::string(g_get_user_data_dir()) + "/mzSpecials";
    std::filesystem::create_directories(dir);
    return dir + "/specials.json";
}

static std::string header_file_path()
{
    std::string dir = std::string(g_get_user_data_dir()) + "/mzSpecials";
    std::filesystem::create_directories(dir);
    return dir + "/header.json";
}

static const std::string DEFAULT_HEADER_TEXT  = "Rolling Pin Bakery";
static const std::string DEFAULT_HEADER_COLOR = "#FF1595";

static void save_header(const std::string& text, const std::string& color)
{
    json obj = {{"text", text}, {"color", color}};
    std::ofstream f(header_file_path());
    f << obj.dump(2);
}

struct HeaderData { std::string text; std::string color; };

static HeaderData load_header()
{
    std::ifstream f(header_file_path());
    if (!f.is_open()) return {DEFAULT_HEADER_TEXT, DEFAULT_HEADER_COLOR};
    try {
        json obj = json::parse(f);
        return {
            obj.value("text",  DEFAULT_HEADER_TEXT),
            obj.value("color", DEFAULT_HEADER_COLOR)
        };
    } catch (...) {
        return {DEFAULT_HEADER_TEXT, DEFAULT_HEADER_COLOR};
    }
}

static std::vector<Special> default_specials()
{
    std::vector<Special> v;
    for (int i = 1; i <= 15; ++i)
        v.emplace_back("Special " + std::to_string(i));
    return v;
}

static void save_specials(const std::vector<Special>& specials)
{
    json arr = json::array();
    for (const auto& s : specials)
        arr.push_back({{"text", s.text}, {"color", s.color_hex}});
    std::ofstream f(data_file_path());
    f << arr.dump(2);
}

static std::vector<Special> load_specials()
{
    std::string path = data_file_path();
    std::ifstream f(path);
    if (!f.is_open())
        return default_specials();
    try {
        json arr = json::parse(f);
        std::vector<Special> v;
        for (const auto& item : arr) {
            std::string text  = item.value("text", "");
            std::string color = item.value("color", "#FFFFFF");
            if (!text.empty())
                v.emplace_back(text, color);
        }
        return v.empty() ? default_specials() : v;
    } catch (...) {
        return default_specials();
    }
}

// -----------------------------------------------------------------------
// Scroll state
// -----------------------------------------------------------------------
struct ScrollState {
    Gtk::Box* content_box = nullptr;
    double    offset      = 0.0;
    gint64    last_time   = 0;
    bool      needs_scroll = false;
    bool      expanded    = false; // true once duplicate copy has been added
    int       half_h      = 0;
    int       viewport_h  = 0;
};

static bool on_scroll_tick(const Glib::RefPtr<Gdk::FrameClock>& clock,
                           ScrollState* state,
                           Gtk::Viewport* viewport)
{
    if (!state->needs_scroll)
        return true;

    gint64 now = clock->get_frame_time();
    if (state->last_time != 0) {
        double dt = (now - state->last_time) / 1e6;
        state->offset += SCROLL_SPEED * dt;
        if (state->half_h > 0 && state->offset >= state->half_h)
            state->offset -= state->half_h;
        viewport->get_vadjustment()->set_value(static_cast<int>(state->offset));
    }
    state->last_time = now;
    return true;
}

// -----------------------------------------------------------------------
// UI helpers
// -----------------------------------------------------------------------
static Gdk::RGBA g_black;

static Gtk::Label* make_special_label(const Special& s, const Pango::FontDescription& font)
{
    auto* label = Gtk::manage(new Gtk::Label(s.text));
    label->override_color(s.rgba());
    label->override_background_color(g_black);
    label->override_font(font);
    label->set_halign(Gtk::ALIGN_CENTER);
    label->set_valign(Gtk::ALIGN_CENTER);
    return label;
}

static void add_separator(Gtk::Box* box)
{
    // Render rpbs.svg using librsvg via a DrawingArea
    // SVG is tiny (25.4mm x 3.06mm) — scale it up to a reasonable display size
    const std::string svg_path =
        std::filesystem::canonical("/proc/self/exe").parent_path().string()
        + "/assets/rpbs.svg";

    // Load once, keep a shared_ptr for the draw callback
    auto rsvg_handle = std::shared_ptr<RsvgHandle>(
        rsvg_handle_new_from_file(svg_path.c_str(), nullptr),
        [](RsvgHandle* h){ if (h) g_object_unref(h); });

    // Target display height in pixels; width scales proportionally
    const int display_h = 60;
    double svg_w = 0, svg_h = 0;
    if (rsvg_handle) {
        double out_w = 0, out_h = 0;
        if (rsvg_handle_get_intrinsic_size_in_pixels(rsvg_handle.get(), &out_w, &out_h)
            && out_h > 0) {
            svg_w = out_w * (static_cast<double>(display_h) / out_h);
            svg_h = display_h;
        }
    }

    auto* sep_box = Gtk::manage(new Gtk::Box(Gtk::ORIENTATION_HORIZONTAL, 0));
    sep_box->override_background_color(g_black);
    sep_box->set_halign(Gtk::ALIGN_CENTER);
    sep_box->set_margin_top(10);
    sep_box->set_margin_bottom(10);

    if (rsvg_handle && svg_w > 0) {
        auto* da = Gtk::manage(new Gtk::DrawingArea());
        da->override_background_color(g_black);
        da->set_size_request(static_cast<int>(svg_w), display_h);

        // Capture shared_ptr so handle stays alive with the widget
        da->signal_draw().connect(
            [rsvg_handle, svg_w, svg_h](const Cairo::RefPtr<Cairo::Context>& cr) {
                double out_w = 0, out_h = 0;
                rsvg_handle_get_intrinsic_size_in_pixels(rsvg_handle.get(), &out_w, &out_h);
                if (out_w > 0 && out_h > 0) {
                    double sx = svg_w / out_w;
                    double sy = svg_h / out_h;
                    cr->scale(sx, sy);
                }
                RsvgRectangle viewport = { 0, 0, out_w > 0 ? out_w : svg_w,
                                                  out_h > 0 ? out_h : svg_h };
                rsvg_handle_render_document(rsvg_handle.get(), cr->cobj(), &viewport, nullptr);
                return false;
            });
        sep_box->pack_start(*da, false, false, 0);
    } else {
        // Fallback: pink rule if SVG can't load
        auto* rule = Gtk::manage(new Gtk::DrawingArea());
        rule->override_background_color(g_black);
        rule->set_size_request(400, 2);
        rule->signal_draw().connect([](const Cairo::RefPtr<Cairo::Context>& cr) {
            cr->set_source_rgb(1.0, 0.08, 0.58);
            cr->set_line_width(2);
            cr->move_to(0, 1);
            cr->line_to(400, 1);
            cr->stroke();
            return false;
        });
        sep_box->pack_start(*rule, false, false, 0);
    }

    box->pack_start(*sep_box, false, false, 0);
}

static void populate_content_box(Gtk::Box* box,
                                 const std::vector<Special>& specials,
                                 const Pango::FontDescription& font)
{
    // Remove existing children
    for (auto* child : box->get_children())
        box->remove(*child);

    // Single copy only — duplicate is added later if scrolling is needed
    for (const auto& s : specials)
        box->pack_start(*make_special_label(s, font), false, false, 6);

    box->show_all();
}

static void expand_for_scroll(Gtk::Box* box,
                              const std::vector<Special>& specials,
                              const Pango::FontDescription& font)
{
    add_separator(box);
    for (const auto& s : specials)
        box->pack_start(*make_special_label(s, font), false, false, 6);
    box->show_all();
}

// -----------------------------------------------------------------------
// Global mutable state (protected by mutex)
// -----------------------------------------------------------------------
static std::mutex          g_mutex;
static std::vector<Special> g_specials;

// GTK widget pointers set during main (only touched on GTK thread)
static Gtk::Box*           g_content_box = nullptr;
static ScrollState*        g_state       = nullptr;
static Pango::FontDescription* g_item_font = nullptr;
static Gtk::Label*         g_header_label = nullptr;
static std::string         g_header_text;
static std::string         g_header_color = DEFAULT_HEADER_COLOR;

// Schedule a header text update on the GTK main thread
static void schedule_header_update()
{
    Glib::signal_idle().connect_once([]() {
        std::string text;
        std::string color;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            text  = g_header_text;
            color = g_header_color;
        }
        if (g_header_label) {
            // Escape for Pango markup
            gchar* escaped_text  = g_markup_escape_text(text.c_str(),  -1);
            gchar* escaped_color = g_markup_escape_text(color.c_str(), -1);
            std::string markup =
                std::string("<span font_family='URW Bookman' style='italic' "
                            "weight='demibold' size='126000' color='") +
                escaped_color + "'>" + escaped_text + "</span>";
            g_free(escaped_text);
            g_free(escaped_color);
            g_header_label->set_markup(markup);
        }
    });
}

// Schedule a UI rebuild on the GTK main thread
static void schedule_rebuild()
{
    Glib::signal_idle().connect_once([]() {
        std::vector<Special> snap;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            snap = g_specials;
        }
        populate_content_box(g_content_box, snap, *g_item_font);
        // Reset scroll state — size_allocate will re-evaluate if expansion is needed
        if (g_state) {
            g_state->offset      = 0.0;
            g_state->last_time   = 0;
            g_state->needs_scroll = false;
            g_state->half_h      = 0;
            g_state->expanded    = false;
        }
    });
}

// -----------------------------------------------------------------------
// HTTP API server (runs in a background thread)
// -----------------------------------------------------------------------
static void run_api_server()
{
    httplib::Server svr;

    // GET /header — return current header text and color
    svr.Get("/header", [](const httplib::Request&, httplib::Response& res) {
        std::lock_guard<std::mutex> lk(g_mutex);
        json obj = {{"text", g_header_text}, {"color", g_header_color}};
        res.set_content(obj.dump(2), "application/json");
    });

    // POST /header — set header text and/or color
    // Body: {"text": "My Bakery", "color": "#FF0000"}
    svr.Post("/header", [](const httplib::Request& req, httplib::Response& res) {
        try {
            json obj = json::parse(req.body);
            std::string text  = obj.value("text",  "");
            std::string color = obj.value("color", "");
            if (text.empty() && color.empty()) {
                res.status = 400;
                res.set_content("{\"error\":\"text or color field required\"}", "application/json");
                return;
            }
            {
                std::lock_guard<std::mutex> lk(g_mutex);
                if (!text.empty())  g_header_text  = text;
                if (!color.empty()) g_header_color = color;
            }
            save_header(g_header_text, g_header_color);
            schedule_header_update();
            res.set_content("{\"status\":\"ok\"}", "application/json");
        } catch (const std::exception& e) {
            res.status = 400;
            res.set_content(std::string("{\"error\":\"")+e.what()+"\"}", "application/json");
        }
    });

    // GET /specials — return current list
    svr.Get("/specials", [](const httplib::Request&, httplib::Response& res) {
        std::lock_guard<std::mutex> lk(g_mutex);
        json arr = json::array();
        for (const auto& s : g_specials)
            arr.push_back({{"text", s.text}, {"color", s.color_hex}});
        res.set_content(arr.dump(2), "application/json");
    });

    // DELETE /specials — clear the list
    svr.Delete("/specials", [](const httplib::Request&, httplib::Response& res) {
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            g_specials.clear();
        }
        save_specials({});
        schedule_rebuild();
        res.set_content("{\"status\":\"cleared\"}", "application/json");
    });

    // POST /specials — replace the list
    // Body: [{"text":"...", "color":"#RRGGBB"}, ...]
    svr.Post("/specials", [](const httplib::Request& req, httplib::Response& res) {
        try {
            json arr = json::parse(req.body);
            std::vector<Special> newlist;
            for (const auto& item : arr) {
                std::string text  = item.value("text", "");
                std::string color = item.value("color", "#FFFFFF");
                if (!text.empty())
                    newlist.emplace_back(text, color);
            }
            {
                std::lock_guard<std::mutex> lk(g_mutex);
                g_specials = newlist;
            }
            save_specials(newlist);
            schedule_rebuild();
            res.set_content("{\"status\":\"ok\",\"count\":" +
                            std::to_string(newlist.size()) + "}", "application/json");
        } catch (const std::exception& e) {
            res.status = 400;
            res.set_content(std::string("{\"error\":\"") + e.what() + "\"}", "application/json");
        }
    });

    svr.listen("0.0.0.0", API_PORT);
}

// -----------------------------------------------------------------------
// main
// -----------------------------------------------------------------------
int main(int argc, char* argv[])
{
    auto app = Gtk::Application::create(argc, argv, "com.terminal-solutions.mzSpecials");

    // Load persisted list
    {
        auto loaded = load_specials();
        std::lock_guard<std::mutex> lk(g_mutex);
        g_specials = loaded;
        auto hdr = load_header();
        g_header_text  = hdr.text;
        g_header_color = hdr.color;
    }

    g_black.set_rgba(0.0, 0.0, 0.0, 1.0);

    // Font for specials
    Pango::FontDescription item_font;
    item_font.set_size(48 * PANGO_SCALE);
    g_item_font = &item_font;

    Gtk::Window window;
    window.set_title("mzSpecials");
    window.fullscreen();
    window.override_background_color(g_black);

    Gtk::Box main_box(Gtk::ORIENTATION_VERTICAL, 0);
    window.add(main_box);

    // --- Header ---
    Gtk::EventBox header_eb;
    header_eb.override_background_color(g_black);

    Gtk::Label header_label;
    {
        gchar* escaped = g_markup_escape_text(g_header_text.c_str(), -1);
        std::string markup =
            std::string("<span font_family='URW Bookman' style='italic' "
                        "weight='demibold' size='126000' color='#FF1595'>") +
            escaped + "</span>";
        g_free(escaped);
        header_label.set_markup(markup);
    }
    g_header_label = &header_label;
    header_label.set_halign(Gtk::ALIGN_CENTER);
    header_label.set_valign(Gtk::ALIGN_CENTER);

    auto header_css = Gtk::CssProvider::create();
    header_css->load_from_data("label { color: #FF1595; background-color: transparent; }");
    header_label.get_style_context()->add_provider(
        header_css, GTK_STYLE_PROVIDER_PRIORITY_APPLICATION + 1);

    header_eb.add(header_label);
    main_box.pack_start(header_eb, false, false, 0);

    // --- Scrolling area ---
    Gtk::ScrolledWindow scrolled;
    scrolled.set_policy(Gtk::POLICY_NEVER, Gtk::POLICY_AUTOMATIC);
    scrolled.override_background_color(g_black);

    auto scroll_css = Gtk::CssProvider::create();
    scroll_css->load_from_data("scrollbar { min-width:0; min-height:0; opacity:0; }"
                               "scrolledwindow > * { background-color: black; }");
    Gtk::StyleContext::add_provider_for_screen(
        Gdk::Screen::get_default(), scroll_css, GTK_STYLE_PROVIDER_PRIORITY_APPLICATION);

    Gtk::Viewport* viewport = Gtk::manage(new Gtk::Viewport(
        Glib::RefPtr<Gtk::Adjustment>(), Glib::RefPtr<Gtk::Adjustment>()));
    viewport->override_background_color(g_black);
    scrolled.add(*viewport);

    Gtk::Box* content_box = Gtk::manage(new Gtk::Box(Gtk::ORIENTATION_VERTICAL, 0));
    content_box->override_background_color(g_black);
    viewport->add(*content_box);
    g_content_box = content_box;

    // Populate initial list
    {
        std::lock_guard<std::mutex> lk(g_mutex);
        populate_content_box(content_box, g_specials, item_font);
    }

    main_box.pack_start(scrolled, true, true, 0);

    ScrollState* state = new ScrollState();
    state->content_box = content_box;
    g_state = state;

    window.signal_realize().connect([&]() {
        int h = window.get_screen()->get_height();
        header_eb.set_size_request(-1, h / 10);
    });

    scrolled.signal_size_allocate().connect([state, &scrolled](Gtk::Allocation&) {
        int content_h = state->content_box->get_allocated_height();
        int view_h    = scrolled.get_allocated_height();
        if (content_h <= 0 || view_h <= 0)
            return;

        if (!state->expanded) {
            if (content_h > view_h) {
                // List overflows — add separator + duplicate for seamless loop
                std::vector<Special> snap;
                {
                    std::lock_guard<std::mutex> lk(g_mutex);
                    snap = g_specials;
                }
                expand_for_scroll(g_content_box, snap, *g_item_font);
                state->expanded = true;
            } else {
                // Fits on screen — single copy, no scroll
                state->needs_scroll = false;
                state->half_h = 0;
                return;
            }
        }

        // Re-measure after potential expansion
        int total_h = state->content_box->get_allocated_height();
        state->half_h       = total_h / 2;
        state->viewport_h   = view_h;
        state->needs_scroll = state->half_h > view_h;
    });

    scrolled.add_tick_callback([state, viewport](const Glib::RefPtr<Gdk::FrameClock>& clock) {
        return on_scroll_tick(clock, state, viewport);
    });

    // Start API server in background thread
    std::thread api_thread(run_api_server);
    api_thread.detach();

    window.show_all();
    int ret = app->run(window);
    delete state;
    return ret;
}
