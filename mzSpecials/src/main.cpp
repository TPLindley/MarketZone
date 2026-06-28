#include <gtkmm.h>
#include <pangomm.h>
#include <pango/pangocairo.h>
#include <librsvg/rsvg.h>
#include <fontconfig/fontconfig.h>
#include <algorithm>
#include <chrono>
#include <cmath>
#include <ctime>
#include <iomanip>
#include <sstream>
#include <vector>
#include <string>
#include <mutex>
#include <thread>
#include <fstream>
#include <filesystem>
#include <functional>
#include <memory>
#include "json.hpp"
#include "httplib.h"

using json = nlohmann::json;

// -----------------------------------------------------------------------
// Logging
// -----------------------------------------------------------------------
static std::mutex g_log_mutex;

static void log(const std::string& level, const std::string& msg)
{
    auto now = std::chrono::system_clock::now();
    std::time_t t = std::chrono::system_clock::to_time_t(now);
    std::tm tm_buf;
    localtime_r(&t, &tm_buf);

    std::lock_guard<std::mutex> lk(g_log_mutex);
    std::clog << std::put_time(&tm_buf, "%H:%M:%S")
              << " [" << level << "] "
              << msg << '\n';
    std::clog.flush();
}

// --- API port ---
static const int API_PORT = 8765;
static const std::string DEFAULT_API_TOKEN = "rpbs$best-cinnamon-buns-ever$";

// --- Scroll speed ---
static const double SCROLL_SPEED = 40.0;
static const int PORTRAIT_SPECIAL_WIDTH = 92;
static const int PORTRAIT_SPECIAL_PADDING = 2;
static const int PORTRAIT_SPECIAL_SLOT = PORTRAIT_SPECIAL_WIDTH + PORTRAIT_SPECIAL_PADDING * 2;

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

static std::string orientation_file_path()
{
    std::string dir = std::string(g_get_user_data_dir()) + "/mzSpecials";
    std::filesystem::create_directories(dir);
    return dir + "/orientation.json";
}

static std::string blanking_file_path()
{
    std::string dir = std::string(g_get_user_data_dir()) + "/mzSpecials";
    std::filesystem::create_directories(dir);
    return dir + "/blanking.json";
}

static const std::string DEFAULT_HEADER_TEXT  = "Rolling Pin Bakery";
static const std::string BRAND_GRAPHIC_COLOR  = "#FF00FF";
static const std::string DEFAULT_HEADER_COLOR = BRAND_GRAPHIC_COLOR;
static const std::string LEGACY_HEADER_COLOR  = "#FF1595";
static const std::string DEFAULT_ORIENTATION  = "landscape";
static const int DEFAULT_BLANK_INTERVAL_SECONDS = 300;
static const double DEFAULT_BLANK_ANIMATION_SECONDS = 3.0;
static const double DEFAULT_BLANK_PAUSE_SECONDS = 2.0;
static const double LEGACY_BLANK_ANIMATION_SECONDS = 5.0;

struct BlankingConfig {
    int interval_seconds = DEFAULT_BLANK_INTERVAL_SECONDS;
    double animation_seconds = DEFAULT_BLANK_ANIMATION_SECONDS;
    double pause_seconds = DEFAULT_BLANK_PAUSE_SECONDS;
};

static BlankingConfig normalize_blanking_config(const BlankingConfig& cfg)
{
    BlankingConfig normalized = cfg;
    if (normalized.interval_seconds <= 0)
        normalized.interval_seconds = DEFAULT_BLANK_INTERVAL_SECONDS;
    if (normalized.animation_seconds <= 0.0)
        normalized.animation_seconds = DEFAULT_BLANK_ANIMATION_SECONDS;
    if (normalized.pause_seconds < 0.0)
        normalized.pause_seconds = DEFAULT_BLANK_PAUSE_SECONDS;
    return normalized;
}

static std::string normalize_orientation(std::string orientation)
{
    std::transform(orientation.begin(), orientation.end(), orientation.begin(), ::tolower);
    return orientation == "portrait" ? "portrait" : DEFAULT_ORIENTATION;
}

static void save_header(const std::string& text, const std::string& color)
{
    log("INFO", "Saving header: text=\"" + text + "\" color=" + color);
    json obj = {{"text", text}, {"color", color}};
    std::ofstream f(header_file_path());
    f << obj.dump(2);
}

struct HeaderData { std::string text; std::string color; };

static HeaderData load_header()
{
    std::ifstream f(header_file_path());
    if (!f.is_open()) {
        log("INFO", "No header file found, using defaults");
        return {DEFAULT_HEADER_TEXT, DEFAULT_HEADER_COLOR};
    }
    try {
        json obj = json::parse(f);
        std::string color = obj.value("color", DEFAULT_HEADER_COLOR);
        if (color == LEGACY_HEADER_COLOR) {
            log("INFO", "Migrating legacy header color " + LEGACY_HEADER_COLOR + " -> " + DEFAULT_HEADER_COLOR);
            color = DEFAULT_HEADER_COLOR;
            // Write back so this migration only happens once
            json updated = {{"text", obj.value("text", DEFAULT_HEADER_TEXT)}, {"color", color}};
            std::ofstream fw(header_file_path());
            fw << updated.dump(2);
        }
        HeaderData hd = {obj.value("text", DEFAULT_HEADER_TEXT), color};
        log("INFO", "Loaded header: text=\"" + hd.text + "\" color=" + hd.color);
        return hd;
    } catch (...) {
        log("WARN", "Failed to parse header file, using defaults");
        return {DEFAULT_HEADER_TEXT, DEFAULT_HEADER_COLOR};
    }
}

static void save_orientation(const std::string& orientation)
{
    log("INFO", "Saving orientation: " + orientation);
    json obj = {{"orientation", normalize_orientation(orientation)}};
    std::ofstream f(orientation_file_path());
    f << obj.dump(2);
}

static std::string load_orientation()
{
    std::ifstream f(orientation_file_path());
    if (!f.is_open()) {
        log("INFO", "No orientation file found, using default: " + DEFAULT_ORIENTATION);
        return DEFAULT_ORIENTATION;
    }

    try {
        json obj = json::parse(f);
        std::string orientation = normalize_orientation(obj.value("orientation", DEFAULT_ORIENTATION));
        log("INFO", "Loaded orientation: " + orientation);
        return orientation;
    } catch (...) {
        log("WARN", "Failed to parse orientation file, using default: " + DEFAULT_ORIENTATION);
        return DEFAULT_ORIENTATION;
    }
}

static void save_blanking_config(const BlankingConfig& cfg)
{
    BlankingConfig normalized = normalize_blanking_config(cfg);
    log("INFO", "Saving blanking config: interval="
        + std::to_string(normalized.interval_seconds)
        + "s animation=" + std::to_string(normalized.animation_seconds)
        + "s pause=" + std::to_string(normalized.pause_seconds) + "s");

    json obj = {
        {"interval_seconds", normalized.interval_seconds},
        {"animation_seconds", normalized.animation_seconds},
        {"pause_seconds", normalized.pause_seconds}
    };
    std::ofstream f(blanking_file_path());
    f << obj.dump(2);
}

static BlankingConfig load_blanking_config()
{
    std::ifstream f(blanking_file_path());
    if (!f.is_open()) {
        log("INFO", "No blanking config file found, using defaults");
        return BlankingConfig{};
    }

    try {
        json obj = json::parse(f);
        BlankingConfig cfg;
        cfg.interval_seconds = obj.value("interval_seconds", DEFAULT_BLANK_INTERVAL_SECONDS);
        cfg.animation_seconds = obj.value("animation_seconds", DEFAULT_BLANK_ANIMATION_SECONDS);
        cfg.pause_seconds = obj.value("pause_seconds", DEFAULT_BLANK_PAUSE_SECONDS);

        bool migrated_legacy_animation = false;
        if (std::abs(cfg.animation_seconds - LEGACY_BLANK_ANIMATION_SECONDS) < 0.0001) {
            cfg.animation_seconds = DEFAULT_BLANK_ANIMATION_SECONDS;
            migrated_legacy_animation = true;
        }

        cfg = normalize_blanking_config(cfg);
        log("INFO", "Loaded blanking config: interval="
            + std::to_string(cfg.interval_seconds)
            + "s animation=" + std::to_string(cfg.animation_seconds)
            + "s pause=" + std::to_string(cfg.pause_seconds) + "s");

        if (migrated_legacy_animation)
            save_blanking_config(cfg);

        return cfg;
    } catch (...) {
        log("WARN", "Failed to parse blanking config, using defaults");
        return BlankingConfig{};
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
    log("INFO", "Saving " + std::to_string(specials.size()) + " specials to " + data_file_path());
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
    if (!f.is_open()) {
        log("INFO", "No specials file at " + path + ", using defaults");
        return default_specials();
    }
    try {
        json arr = json::parse(f);
        std::vector<Special> v;
        for (const auto& item : arr) {
            std::string text  = item.value("text", "");
            std::string color = item.value("color", "#FFFFFF");
            if (!text.empty())
                v.emplace_back(text, color);
        }
        if (v.empty()) {
            log("WARN", "Specials file was empty, using defaults");
            return default_specials();
        }
        log("INFO", "Loaded " + std::to_string(v.size()) + " specials from " + path);
        return v;
    } catch (...) {
        log("WARN", "Failed to parse specials file at " + path + ", using defaults");
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
    int       first_copy_h = 0;
    int       loop_h      = 0;
    int       viewport_h  = 0;
    int       settle_frames = 0;
};

static bool is_portrait_orientation();

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
        if (state->loop_h > 0 && state->offset >= state->loop_h)
            state->offset = 0.0;

        auto adjustment = is_portrait_orientation()
            ? viewport->get_hadjustment()
            : viewport->get_vadjustment();
        double max_value = std::max(0.0, adjustment->get_upper() - adjustment->get_page_size());
        if (max_value > 0.0)
            adjustment->set_value(std::min(state->offset, max_value));
    }
    state->last_time = now;
    return true;
}

// -----------------------------------------------------------------------
// UI helpers
// -----------------------------------------------------------------------
static Gdk::RGBA g_black;

static Gtk::Label* make_special_label(const Special& s,
                                      const Pango::FontDescription& font,
                                      bool portrait)
{
    auto* label = Gtk::manage(new Gtk::Label(s.text));
    label->override_color(s.rgba());
    label->override_background_color(g_black);
    label->override_font(font);
    label->set_angle(portrait ? 90.0 : 0.0);
    if (portrait)
        label->set_size_request(PORTRAIT_SPECIAL_WIDTH, -1);
    label->set_halign(Gtk::ALIGN_CENTER);
    label->set_valign(Gtk::ALIGN_CENTER);
    return label;
}

static void add_separator(Gtk::Box* box, bool portrait)
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
    const int display_h = portrait ? 42 : 60;
    double svg_w = 0, svg_h = 0;
    if (rsvg_handle) {
        double out_w = 0, out_h = 0;
        if (rsvg_handle_get_intrinsic_size_in_pixels(rsvg_handle.get(), &out_w, &out_h)
            && out_h > 0) {
            svg_w = out_w * (static_cast<double>(display_h) / out_h);
            svg_h = display_h;
        }
    }

    auto* sep_box = Gtk::manage(new Gtk::Box(portrait ? Gtk::ORIENTATION_VERTICAL
                                                      : Gtk::ORIENTATION_HORIZONTAL, 0));
    sep_box->override_background_color(g_black);
    sep_box->set_halign(Gtk::ALIGN_CENTER);
    sep_box->set_valign(Gtk::ALIGN_CENTER);
    sep_box->set_margin_top(portrait ? 0 : 10);
    sep_box->set_margin_bottom(portrait ? 0 : 10);
    sep_box->set_margin_left(portrait ? 8 : 0);
    sep_box->set_margin_right(portrait ? 8 : 0);

    // Helper: creates a horizontal rule matching the separator SVG color,
    // vertically centred within display_h pixels.
    auto make_pink_line = [display_h]() {
        auto* line = Gtk::manage(new Gtk::DrawingArea());
        line->override_background_color(Gdk::RGBA("black"));
        line->set_size_request(1, display_h);  // width expands via pack_start expand
        line->signal_draw().connect([line, display_h](const Cairo::RefPtr<Cairo::Context>& cr) {
            int w = line->get_allocated_width();
            double y = display_h / 2.0;
            cr->set_source_rgb(1.0, 0.0, 1.0);
            cr->set_line_width(3);
            cr->move_to(0, y);
            cr->line_to(w, y);
            cr->stroke();
            return false;
        });
        return line;
    };

    auto make_vertical_pink_line = []() {
        auto* line = Gtk::manage(new Gtk::DrawingArea());
        line->override_background_color(Gdk::RGBA("black"));
        line->set_size_request(34, 1);
        line->signal_draw().connect([line](const Cairo::RefPtr<Cairo::Context>& cr) {
            int h = line->get_allocated_height();
            double x = line->get_allocated_width() / 2.0;
            cr->set_source_rgb(1.0, 0.0, 1.0);
            cr->set_line_width(3);
            cr->move_to(x, 0);
            cr->line_to(x, h);
            cr->stroke();
            return false;
        });
        return line;
    };

    if (rsvg_handle && svg_w > 0) {
        auto* da = Gtk::manage(new Gtk::DrawingArea());
        da->override_background_color(g_black);
        da->set_size_request(
            portrait ? display_h : static_cast<int>(svg_w),
            portrait ? static_cast<int>(svg_w) : display_h);

        // Capture shared_ptr so handle stays alive with the widget
        da->signal_draw().connect(
            [rsvg_handle, svg_w, svg_h, portrait](const Cairo::RefPtr<Cairo::Context>& cr) {
                double out_w = 0, out_h = 0;
                rsvg_handle_get_intrinsic_size_in_pixels(rsvg_handle.get(), &out_w, &out_h);
                if (portrait) {
                    cr->translate(0, svg_w);
                    cr->rotate(-3.14159265358979323846 / 2.0);
                }
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

        if (portrait) {
            sep_box->set_valign(Gtk::ALIGN_FILL);
            sep_box->pack_start(*make_vertical_pink_line(), true, true, 6);
            sep_box->pack_start(*da, false, false, 0);
            sep_box->pack_start(*make_vertical_pink_line(), true, true, 6);
        } else {
            sep_box->set_halign(Gtk::ALIGN_FILL);
            sep_box->pack_start(*make_pink_line(), true, true, 8);
            sep_box->pack_start(*da, false, false, 0);
            sep_box->pack_start(*make_pink_line(), true, true, 8);
        }
    } else {
        // Fallback: graphic-color rule if SVG can't load
        auto* rule = Gtk::manage(new Gtk::DrawingArea());
        rule->override_background_color(g_black);
        rule->set_size_request(portrait ? 34 : 400, portrait ? 400 : 2);
        rule->signal_draw().connect([portrait, rule](const Cairo::RefPtr<Cairo::Context>& cr) {
            cr->set_source_rgb(1.0, 0.0, 1.0);
            cr->set_line_width(2);
            if (portrait) {
                double x = rule->get_allocated_width() / 2.0;
                cr->move_to(x, 0);
                cr->line_to(x, rule->get_allocated_height());
            } else {
                cr->move_to(0, 1);
                cr->line_to(rule->get_allocated_width(), 1);
            }
            cr->stroke();
            return false;
        });
        sep_box->pack_start(*rule, false, false, 0);
    }

    box->pack_start(*sep_box, false, false, 0);
}

static void populate_content_box(Gtk::Box* box,
                                 const std::vector<Special>& specials,
                                 const Pango::FontDescription& font,
                                 bool portrait)
{
    // Remove existing children
    for (auto* child : box->get_children())
        box->remove(*child);

    box->set_orientation(portrait ? Gtk::ORIENTATION_HORIZONTAL : Gtk::ORIENTATION_VERTICAL);

    // Single copy only — duplicate is added later if scrolling is needed
    for (const auto& s : specials)
        box->pack_start(*make_special_label(s, font, portrait), false, false, portrait ? 2 : 6);

    box->show_all();
}

static void expand_for_scroll(Gtk::Box* box,
                              const std::vector<Special>& specials,
                              const Pango::FontDescription& font,
                              bool portrait)
{
    add_separator(box, portrait);
    for (const auto& s : specials)
        box->pack_start(*make_special_label(s, font, portrait), false, false, portrait ? 2 : 6);
    box->show_all();
}

// -----------------------------------------------------------------------
// Global mutable state (protected by mutex)
// -----------------------------------------------------------------------
static std::mutex          g_mutex;
static std::vector<Special> g_specials;

// GTK widget pointers set during main (only touched on GTK thread)
static Gtk::Box*           g_content_box = nullptr;
static Gtk::Box*           g_main_box    = nullptr;
static Gtk::EventBox*      g_header_box  = nullptr;
static Gtk::ScrolledWindow* g_scrolled_window = nullptr;
static Gtk::Viewport*      g_viewport    = nullptr;
static ScrollState*        g_state       = nullptr;
static Pango::FontDescription* g_item_font = nullptr;
static Gtk::DrawingArea*   g_header_area = nullptr;
static Gtk::DrawingArea*   g_blanking_area = nullptr;
static std::string         g_header_text;
static std::string         g_header_color = DEFAULT_HEADER_COLOR;
static std::string         g_orientation = DEFAULT_ORIENTATION;
static std::string         g_header_font_family = "DejaVu Serif";
static bool                g_using_custom_header_font = false;
static bool                g_header_font_bold = true;
static const int           HEADER_FONT_SIZE = 990000;
static const std::string   PREFERRED_HEADER_FONT_FAMILY = "Merriweather";
static const double        PI = 3.14159265358979323846;
static BlankingConfig      g_blanking_config;
static bool                g_blanking_active = false;
static std::chrono::steady_clock::time_point g_next_blank_time;
static std::chrono::steady_clock::time_point g_blanking_started_at;
static std::shared_ptr<RsvgHandle> g_separator_svg_handle;
static double              g_separator_svg_w = 0.0;
static double              g_separator_svg_h = 0.0;
static std::string         g_api_token = DEFAULT_API_TOKEN;

static void schedule_rebuild();
static void evaluate_scroll_state();
static void schedule_scroll_evaluations(int count);

static std::string load_api_token()
{
    const char* env_token = g_getenv("MZSPECIALS_API_TOKEN");
    if (env_token && *env_token)
        return env_token;
    return DEFAULT_API_TOKEN;
}

static bool request_has_valid_token(const httplib::Request& req)
{
    std::string expected_token;
    {
        std::lock_guard<std::mutex> lk(g_mutex);
        expected_token = g_api_token;
    }

    if (expected_token.empty())
        return false;

    std::string token = req.get_header_value("X-API-Token");
    if (token.empty()) {
        const std::string auth = req.get_header_value("Authorization");
        const std::string bearer_prefix = "Bearer ";
        if (auth.rfind(bearer_prefix, 0) == 0)
            token = auth.substr(bearer_prefix.size());
    }

    return token == expected_token;
}

static bool load_separator_svg_asset()
{
    const std::string svg_path =
        std::filesystem::canonical("/proc/self/exe").parent_path().string()
        + "/assets/rpbs.svg";

    auto handle = std::shared_ptr<RsvgHandle>(
        rsvg_handle_new_from_file(svg_path.c_str(), nullptr),
        [](RsvgHandle* h){ if (h) g_object_unref(h); });
    if (!handle)
        return false;

    double out_w = 0.0;
    double out_h = 0.0;
    if (!rsvg_handle_get_intrinsic_size_in_pixels(handle.get(), &out_w, &out_h)
        || out_w <= 0.0 || out_h <= 0.0)
        return false;

    g_separator_svg_handle = handle;
    g_separator_svg_w = out_w;
    g_separator_svg_h = out_h;
    return true;
}

static bool update_blanking_animation()
{
    bool should_queue_draw = false;
    bool should_be_visible = false;

    {
        std::lock_guard<std::mutex> lk(g_mutex);
        auto now = std::chrono::steady_clock::now();

        if (!g_blanking_active && now >= g_next_blank_time) {
            g_blanking_active = true;
            g_blanking_started_at = now;
            should_queue_draw = true;
        }

        if (g_blanking_active) {
            should_be_visible = true;
            should_queue_draw = true;

            std::chrono::duration<double> elapsed = now - g_blanking_started_at;
            if (elapsed.count() >= (g_blanking_config.animation_seconds + g_blanking_config.pause_seconds)) {
                g_blanking_active = false;
                should_be_visible = false;
                g_next_blank_time = now + std::chrono::seconds(g_blanking_config.interval_seconds);
            }
        }
    }

    if (g_blanking_area) {
        g_blanking_area->set_visible(should_be_visible);
        if (should_queue_draw)
            g_blanking_area->queue_draw();
    }

    return true;
}

static std::string load_custom_header_font_family()
{
    const std::filesystem::path assets_dir =
        std::filesystem::canonical("/proc/self/exe").parent_path() / "assets";

    if (!std::filesystem::exists(assets_dir))
        return "";

    std::vector<std::filesystem::path> font_paths;
    for (const auto& entry : std::filesystem::directory_iterator(assets_dir)) {
        if (!entry.is_regular_file())
            continue;

        std::string ext = entry.path().extension().string();
        std::transform(ext.begin(), ext.end(), ext.begin(), ::tolower);
        if (ext == ".ttf" || ext == ".otf")
            font_paths.push_back(entry.path());
    }

    std::sort(font_paths.begin(), font_paths.end());

    std::string fallback_family;
    bool found_preferred = false;

    for (const auto& font_path : font_paths) {
        const std::string path = font_path.string();
        int font_index = 0;
        FcPattern* pattern = FcFreeTypeQuery(
            reinterpret_cast<const FcChar8*>(path.c_str()),
            0,
            nullptr,
            &font_index);
        if (!pattern)
            continue;

        FcChar8* family = nullptr;
        std::string family_name;
        if (FcPatternGetString(pattern, FC_FAMILY, 0, &family) == FcResultMatch)
            family_name = reinterpret_cast<const char*>(family);

        FcPatternDestroy(pattern);

        if (family_name.empty())
            continue;

        if (FcConfigAppFontAddFile(
                FcConfigGetCurrent(),
                reinterpret_cast<const FcChar8*>(path.c_str()))) {
            if (fallback_family.empty())
                fallback_family = family_name;
            if (family_name == PREFERRED_HEADER_FONT_FAMILY)
                found_preferred = true;
        }
    }

    FcConfigBuildFonts(FcConfigGetCurrent());

    if (found_preferred)
        return PREFERRED_HEADER_FONT_FAMILY;

    return fallback_family;
}

static Pango::FontDescription make_header_font(int size = HEADER_FONT_SIZE)
{
    Pango::FontDescription font;
    font.set_family(g_header_font_family);
    font.set_style(g_using_custom_header_font ? Pango::STYLE_NORMAL : Pango::STYLE_ITALIC);
    font.set_weight(g_header_font_bold ? Pango::WEIGHT_BOLD : Pango::WEIGHT_NORMAL);
    font.set_size(size);
    return font;
}

static Gdk::RGBA parse_header_color(const std::string& color)
{
    Gdk::RGBA rgba;
    if (!rgba.set(color))
        rgba.set(DEFAULT_HEADER_COLOR);
    return rgba;
}

static void reset_scroll_state()
{
    if (!g_state)
        return;

    g_state->offset       = 0.0;
    g_state->last_time    = 0;
    g_state->needs_scroll = false;
    g_state->first_copy_h = 0;
    g_state->loop_h       = 0;
    g_state->expanded     = false;
    g_state->settle_frames = 8;
    if (g_viewport) {
        g_viewport->get_hadjustment()->set_value(0);
        g_viewport->get_vadjustment()->set_value(0);
    }
}

static bool is_portrait_orientation()
{
    std::lock_guard<std::mutex> lk(g_mutex);
    return g_orientation == "portrait";
}

static void evaluate_scroll_state()
{
    if (!g_state || !g_scrolled_window || !g_content_box || !g_item_font || !g_viewport)
        return;

    bool portrait = is_portrait_orientation();
    int content_size = portrait
        ? g_state->content_box->get_allocated_width()
        : g_state->content_box->get_allocated_height();
    int view_size = portrait
        ? g_scrolled_window->get_allocated_width()
        : g_scrolled_window->get_allocated_height();
    if (content_size <= 0 || view_size <= 0)
        return;

    size_t special_count = 0;
    {
        std::lock_guard<std::mutex> lk(g_mutex);
        special_count = g_specials.size();
    }
    if (special_count == 0) {
        g_state->needs_scroll = false;
        return;
    }

    if (!g_state->expanded) {
        double average_item_size = static_cast<double>(content_size) / special_count;
        int visible_capacity = portrait
            ? std::max(1, view_size / PORTRAIT_SPECIAL_SLOT)
            : (average_item_size > 0.0
                ? std::max(1, static_cast<int>(view_size / average_item_size))
                : 1);

        if (static_cast<int>(special_count) > visible_capacity) {
            log("DEBUG", "Scroll enabled: " + std::to_string(special_count)
                + " items, capacity=" + std::to_string(visible_capacity)
                + " (" + std::string(portrait ? "portrait" : "landscape") + ")");
            g_state->first_copy_h = std::max(
                content_size,
                portrait
                    ? static_cast<int>(PORTRAIT_SPECIAL_SLOT * special_count)
                    : static_cast<int>(average_item_size * special_count));
            std::vector<Special> snap;
            {
                std::lock_guard<std::mutex> lk(g_mutex);
                snap = g_specials;
            }
            expand_for_scroll(g_content_box, snap, *g_item_font, portrait);
            g_state->expanded = true;
            g_state->loop_h = g_state->first_copy_h;
            g_state->viewport_h = view_size;
            g_state->needs_scroll = true;
            return;
        } else {
            if (g_state->needs_scroll) {
                log("DEBUG", "Scroll not needed: " + std::to_string(special_count)
                    + " items fit within capacity=" + std::to_string(visible_capacity));
            }
            g_state->needs_scroll = false;
            g_state->first_copy_h = 0;
            g_state->loop_h = 0;
            return;
        }
    }

    if (g_state->first_copy_h > 0)
        g_state->loop_h = g_state->first_copy_h;
    g_state->viewport_h   = view_size;
    if (g_state->loop_h > 0)
        g_state->needs_scroll = true;
    else
        g_state->needs_scroll = false;
}

static void apply_orientation_layout()
{
    if (!g_main_box || !g_header_box)
        return;

    std::string orientation;
    {
        std::lock_guard<std::mutex> lk(g_mutex);
        orientation = g_orientation;
    }

    auto screen = Gdk::Screen::get_default();
    int screen_w = screen ? screen->get_width() : 1920;
    int screen_h = screen ? screen->get_height() : 1080;

    if (orientation == "portrait") {
        g_main_box->set_orientation(Gtk::ORIENTATION_HORIZONTAL);
        if (g_content_box)
            g_content_box->set_orientation(Gtk::ORIENTATION_HORIZONTAL);
        if (g_scrolled_window)
            g_scrolled_window->set_policy(Gtk::POLICY_AUTOMATIC, Gtk::POLICY_NEVER);
        g_header_box->set_size_request(std::max(140, screen_w / 8), -1);
    } else {
        g_main_box->set_orientation(Gtk::ORIENTATION_VERTICAL);
        if (g_content_box)
            g_content_box->set_orientation(Gtk::ORIENTATION_VERTICAL);
        if (g_scrolled_window)
            g_scrolled_window->set_policy(Gtk::POLICY_NEVER, Gtk::POLICY_AUTOMATIC);
        g_header_box->set_size_request(-1, std::max(120, screen_h / 8));
    }

    if (g_header_area)
        g_header_area->queue_draw();
    if (g_scrolled_window)
        g_scrolled_window->queue_resize();
    reset_scroll_state();
}

static void schedule_orientation_update()
{
    log("INFO", "Orientation layout update scheduled");
    Glib::signal_idle().connect_once([]() {
        apply_orientation_layout();
        schedule_rebuild();
    });
}

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
        if (g_header_area)
            g_header_area->queue_draw();
    });
}

// Schedule a UI rebuild on the GTK main thread
static void schedule_rebuild()
{
    log("DEBUG", "UI rebuild scheduled");
    Glib::signal_idle().connect_once([]() {
        std::vector<Special> snap;
        bool portrait;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            snap = g_specials;
            portrait = g_orientation == "portrait";
        }
        populate_content_box(g_content_box, snap, *g_item_font, portrait);
        // Reset scroll state — size_allocate will re-evaluate if expansion is needed
        reset_scroll_state();
        if (g_content_box)
            g_content_box->queue_resize();
        if (g_scrolled_window)
            g_scrolled_window->queue_resize();
        schedule_scroll_evaluations(6);
    });
}

static void schedule_scroll_evaluations(int count)
{
    if (count <= 0)
        return;

    Glib::signal_idle().connect_once([count]() {
        evaluate_scroll_state();
        schedule_scroll_evaluations(count - 1);
    });
}

// -----------------------------------------------------------------------
// HTTP API server (runs in a background thread)
// -----------------------------------------------------------------------
static void run_api_server()
{
    httplib::Server svr;

    svr.set_pre_routing_handler([](const httplib::Request& req, httplib::Response& res) {
        if (request_has_valid_token(req))
            return httplib::Server::HandlerResponse::Unhandled;

        log("WARN", "Unauthorized request to " + req.path + " from " + req.remote_addr);
        res.status = 401;
        res.set_content("{\"error\":\"unauthorized\"}", "application/json");
        return httplib::Server::HandlerResponse::Handled;
    });

    // GET /header — return current header text and color
    svr.Get("/header", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "GET /header from " + req.remote_addr);
        std::string text, color;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            text  = g_header_text;
            color = g_header_color;
        }
        json obj = {{"text", text}, {"color", color}};
        res.set_content(obj.dump(2), "application/json");
        log("HTTP", "GET /header -> text=\"" + text + "\" color=" + color);
    });

    // GET /orientation — return current display orientation preference
    svr.Get("/orientation", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "GET /orientation from " + req.remote_addr);
        std::string orientation;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            orientation = g_orientation;
        }
        json obj = {{"orientation", orientation}};
        res.set_content(obj.dump(2), "application/json");
        log("HTTP", "GET /orientation -> " + orientation);
    });

    // POST /orientation — set display orientation preference
    // Body: {"orientation": "landscape"} or {"orientation": "portrait"}
    svr.Post("/orientation", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "POST /orientation from " + req.remote_addr + " body=" + req.body);
        try {
            json obj = json::parse(req.body);
            if (!obj.contains("orientation")) {
                log("WARN", "POST /orientation missing orientation field");
                res.status = 400;
                res.set_content("{\"error\":\"orientation field required\"}", "application/json");
                return;
            }

            std::string orientation = normalize_orientation(obj.value("orientation", DEFAULT_ORIENTATION));
            {
                std::lock_guard<std::mutex> lk(g_mutex);
                g_orientation = orientation;
            }
            save_orientation(orientation);
            schedule_orientation_update();
            log("HTTP", "POST /orientation -> set to " + orientation);
            res.set_content(
                (json{{"status", "ok"}, {"orientation", orientation}}).dump(2),
                "application/json");
        } catch (const std::exception& e) {
            log("ERROR", std::string("POST /orientation exception: ") + e.what());
            res.status = 400;
            res.set_content(std::string("{\"error\":\"") + e.what() + "\"}", "application/json");
        }
    });

    // POST /header — set header text and/or color
    // Body: {"text": "My Bakery", "color": "#FF0000"}
    svr.Post("/header", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "POST /header from " + req.remote_addr + " body=" + req.body);
        try {
            json obj = json::parse(req.body);
            std::string text  = obj.value("text",  "");
            std::string color = obj.value("color", "");
            if (text.empty() && color.empty()) {
                log("WARN", "POST /header missing text and color fields");
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
            log("HTTP", "POST /header -> ok");
            res.set_content("{\"status\":\"ok\"}", "application/json");
        } catch (const std::exception& e) {
            log("ERROR", std::string("POST /header exception: ") + e.what());
            res.status = 400;
            res.set_content(std::string("{\"error\":\"")+e.what()+"\"}", "application/json");
        }
    });

    // GET /specials — return current list
    svr.Get("/specials", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "GET /specials from " + req.remote_addr);
        json arr = json::array();
        size_t count = 0;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            for (const auto& s : g_specials)
                arr.push_back({{"text", s.text}, {"color", s.color_hex}});
            count = g_specials.size();
        }
        res.set_content(arr.dump(2), "application/json");
        log("HTTP", "GET /specials -> returned " + std::to_string(count) + " items");
    });

    // DELETE /specials — clear the list
    svr.Delete("/specials", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "DELETE /specials from " + req.remote_addr);
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            g_specials.clear();
        }
        save_specials({});
        schedule_rebuild();
        log("HTTP", "DELETE /specials -> cleared");
        res.set_content("{\"status\":\"cleared\"}", "application/json");
    });

    // POST /specials — replace the list
    // Body: [{"text":"...", "color":"#RRGGBB"}, ...]
    svr.Post("/specials", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "POST /specials from " + req.remote_addr
            + " (" + std::to_string(req.body.size()) + " bytes)");
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
            log("HTTP", "POST /specials -> accepted " + std::to_string(newlist.size()) + " items");
            res.set_content("{\"status\":\"ok\",\"count\":" +
                            std::to_string(newlist.size()) + "}", "application/json");
        } catch (const std::exception& e) {
            log("ERROR", std::string("POST /specials exception: ") + e.what());
            res.status = 400;
            res.set_content(std::string("{\"error\":\"") + e.what() + "\"}", "application/json");
        }
    });

    // GET /blanking — return timer/animation configuration
    svr.Get("/blanking", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "GET /blanking from " + req.remote_addr);
        BlankingConfig cfg;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            cfg = g_blanking_config;
        }
        json obj = {
            {"interval_seconds", cfg.interval_seconds},
            {"animation_seconds", cfg.animation_seconds},
            {"pause_seconds", cfg.pause_seconds}
        };
        res.set_content(obj.dump(2), "application/json");
    });

    // POST /blanking — set timer/animation configuration
    // Body: {"interval_seconds":300,"animation_seconds":3.0,"pause_seconds":2.0}
    svr.Post("/blanking", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "POST /blanking from " + req.remote_addr + " body=" + req.body);
        try {
            json obj = json::parse(req.body);
            BlankingConfig cfg;
            {
                std::lock_guard<std::mutex> lk(g_mutex);
                cfg = g_blanking_config;
            }

            if (obj.contains("interval_seconds")) {
                int interval = obj.value("interval_seconds", cfg.interval_seconds);
                if (interval <= 0) {
                    res.status = 400;
                    res.set_content("{\"error\":\"interval_seconds must be > 0\"}", "application/json");
                    return;
                }
                cfg.interval_seconds = interval;
            }

            if (obj.contains("animation_seconds")) {
                double animation = obj.value("animation_seconds", cfg.animation_seconds);
                if (animation <= 0.0) {
                    res.status = 400;
                    res.set_content("{\"error\":\"animation_seconds must be > 0\"}", "application/json");
                    return;
                }
                cfg.animation_seconds = animation;
            }

            if (obj.contains("pause_seconds")) {
                double pause = obj.value("pause_seconds", cfg.pause_seconds);
                if (pause < 0.0) {
                    res.status = 400;
                    res.set_content("{\"error\":\"pause_seconds must be >= 0\"}", "application/json");
                    return;
                }
                cfg.pause_seconds = pause;
            }

            cfg = normalize_blanking_config(cfg);

            {
                std::lock_guard<std::mutex> lk(g_mutex);
                g_blanking_config = cfg;
                g_blanking_active = false;
                g_next_blank_time = std::chrono::steady_clock::now()
                    + std::chrono::seconds(g_blanking_config.interval_seconds);
            }
            save_blanking_config(cfg);

            json out = {
                {"status", "ok"},
                {"interval_seconds", cfg.interval_seconds},
                {"animation_seconds", cfg.animation_seconds},
                {"pause_seconds", cfg.pause_seconds}
            };
            res.set_content(out.dump(2), "application/json");
        } catch (const std::exception& e) {
            log("ERROR", std::string("POST /blanking exception: ") + e.what());
            res.status = 400;
            res.set_content(std::string("{\"error\":\"") + e.what() + "\"}", "application/json");
        }
    });

    // POST /blanking/trigger — trigger blank/tumble animation immediately
    svr.Post("/blanking/trigger", [](const httplib::Request& req, httplib::Response& res) {
        log("HTTP", "POST /blanking/trigger from " + req.remote_addr);
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            auto now = std::chrono::steady_clock::now();
            g_blanking_active = true;
            g_blanking_started_at = now;
            g_next_blank_time = now + std::chrono::seconds(g_blanking_config.interval_seconds);
        }

        Glib::signal_idle().connect_once([]() {
            if (g_blanking_area) {
                g_blanking_area->set_visible(true);
                g_blanking_area->queue_draw();
            }
        });

        res.set_content("{\"status\":\"ok\",\"triggered\":true}", "application/json");
    });

    if (!svr.bind_to_port("0.0.0.0", API_PORT)) {
        log("ERROR", "Failed to bind to port " + std::to_string(API_PORT)
            + " — port already in use?");
        return;
    }
    log("INFO", "API server ready on all interfaces, port " + std::to_string(API_PORT)
        + " (0.0.0.0 = wlan0 + eth0 + lo)");
    svr.listen_after_bind();
    log("WARN", "API server stopped listening");
}

// -----------------------------------------------------------------------
// main
// -----------------------------------------------------------------------
int main(int argc, char* argv[])
{
    log("INFO", "mzSpecials starting up");
    auto app = Gtk::Application::create(argc, argv, "com.terminal-solutions.mzSpecials");

    {
        std::lock_guard<std::mutex> lk(g_mutex);
        g_api_token = load_api_token();
    }
    if (g_api_token == DEFAULT_API_TOKEN) {
        log("WARN", "Using default API token; set MZSPECIALS_API_TOKEN for production");
    }

    // Load persisted list
    {
        auto loaded = load_specials();
        std::lock_guard<std::mutex> lk(g_mutex);
        g_specials = loaded;
        auto hdr = load_header();
        g_header_text  = hdr.text;
        g_header_color = hdr.color;
        g_orientation  = load_orientation();
        g_blanking_config = load_blanking_config();
        g_next_blank_time = std::chrono::steady_clock::now()
            + std::chrono::seconds(g_blanking_config.interval_seconds);
        g_blanking_active = false;
    }

    g_black.set_rgba(0.0, 0.0, 0.0, 1.0);

    std::string custom_header_font = load_custom_header_font_family();
    if (!custom_header_font.empty()) {
        g_header_font_family = custom_header_font;
        g_using_custom_header_font = true;
        g_header_font_bold = (custom_header_font == PREFERRED_HEADER_FONT_FAMILY);
    }

    // Font for specials
    Pango::FontDescription item_font;
    item_font.set_size(48 * PANGO_SCALE);
    g_item_font = &item_font;

    Gtk::Window window;
    window.set_title("mzSpecials");
    window.fullscreen();
    window.override_background_color(g_black);

    Gtk::Overlay root_overlay;
    window.add(root_overlay);

    Gtk::Box main_box(Gtk::ORIENTATION_VERTICAL, 0);
    g_main_box = &main_box;
    root_overlay.add(main_box);

    // --- Header ---
    Gtk::EventBox header_eb;
    g_header_box = &header_eb;
    header_eb.override_background_color(g_black);

    Gtk::DrawingArea header_area;
    g_header_area = &header_area;
    header_area.override_background_color(g_black);
    header_area.signal_draw().connect([&header_area](const Cairo::RefPtr<Cairo::Context>& cr) {
        std::string text;
        std::string color;
        std::string orientation;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            text = g_header_text;
            color = g_header_color;
            orientation = g_orientation;
        }

        int w = header_area.get_allocated_width();
        int h = header_area.get_allocated_height();
        cr->set_source_rgb(0.0, 0.0, 0.0);
        cr->rectangle(0, 0, w, h);
        cr->fill();

        bool portrait = orientation == "portrait";
        if (portrait) {
            cr->translate(0, h);
            cr->rotate(-PI / 2.0);
            std::swap(w, h);
        }

        auto layout = header_area.create_pango_layout(text);
        layout->set_alignment(Pango::ALIGN_CENTER);

        Pango::Rectangle ink_rect;
        Pango::Rectangle logical_rect;
        int font_size = HEADER_FONT_SIZE;
        const int min_font_size = 48000;
        const int x_margin = 8;
        const int y_margin = 10;
        double ink_x = 0.0;
        double ink_y = 0.0;
        double ink_w = 0.0;
        double ink_h = 0.0;
        do {
            layout->set_font_description(make_header_font(font_size));
            layout->get_extents(ink_rect, logical_rect);
            ink_w = static_cast<double>(ink_rect.get_width()) / PANGO_SCALE;
            ink_h = static_cast<double>(ink_rect.get_height()) / PANGO_SCALE;
            if ((ink_w <= w - x_margin * 2 && ink_h <= h - y_margin * 2) ||
                font_size <= min_font_size)
                break;
            font_size -= 4000;
        } while (true);

        ink_x = static_cast<double>(ink_rect.get_x()) / PANGO_SCALE;
        ink_y = static_cast<double>(ink_rect.get_y()) / PANGO_SCALE;

        Gdk::RGBA rgba = parse_header_color(color);
        cr->set_antialias(Cairo::ANTIALIAS_GRAY);
        cr->set_source_rgba(rgba.get_red(), rgba.get_green(), rgba.get_blue(), rgba.get_alpha());
        cr->move_to((w - ink_w) / 2.0 - ink_x, (h - ink_h) / 2.0 - ink_y);
        pango_cairo_layout_path(cr->cobj(), layout->gobj());
        cr->fill();
        return true;
    });

    header_eb.add(header_area);
    main_box.pack_start(header_eb, false, false, 0);

    // --- Scrolling area ---
    Gtk::ScrolledWindow scrolled;
    g_scrolled_window = &scrolled;
    scrolled.set_policy(Gtk::POLICY_NEVER, Gtk::POLICY_AUTOMATIC);
    scrolled.override_background_color(g_black);

    auto scroll_css = Gtk::CssProvider::create();
    scroll_css->load_from_data("scrollbar { min-width:0; min-height:0; opacity:0; }"
                               "scrolledwindow > * { background-color: black; }");
    Gtk::StyleContext::add_provider_for_screen(
        Gdk::Screen::get_default(), scroll_css, GTK_STYLE_PROVIDER_PRIORITY_APPLICATION);

    Gtk::Viewport* viewport = Gtk::manage(new Gtk::Viewport(
        Glib::RefPtr<Gtk::Adjustment>(), Glib::RefPtr<Gtk::Adjustment>()));
    g_viewport = viewport;
    viewport->override_background_color(g_black);
    scrolled.add(*viewport);

    Gtk::Box* content_box = Gtk::manage(new Gtk::Box(Gtk::ORIENTATION_VERTICAL, 0));
    content_box->override_background_color(g_black);
    viewport->add(*content_box);
    g_content_box = content_box;

    // Populate initial list
    {
        std::lock_guard<std::mutex> lk(g_mutex);
        populate_content_box(content_box, g_specials, item_font, g_orientation == "portrait");
    }

    main_box.pack_start(scrolled, true, true, 0);

    load_separator_svg_asset();

    Gtk::DrawingArea blanking_area;
    g_blanking_area = &blanking_area;
    blanking_area.override_background_color(g_black);
    blanking_area.set_halign(Gtk::ALIGN_FILL);
    blanking_area.set_valign(Gtk::ALIGN_FILL);
    blanking_area.set_hexpand(true);
    blanking_area.set_vexpand(true);
    blanking_area.set_no_show_all(true);
    blanking_area.hide();
    blanking_area.signal_draw().connect([&blanking_area](const Cairo::RefPtr<Cairo::Context>& cr) {
        int w = blanking_area.get_allocated_width();
        int h = blanking_area.get_allocated_height();
        cr->set_source_rgb(0.0, 0.0, 0.0);
        cr->rectangle(0, 0, w, h);
        cr->fill();

        double elapsed_seconds = 0.0;
        double animation_seconds = DEFAULT_BLANK_ANIMATION_SECONDS;
        bool portrait = false;
        {
            std::lock_guard<std::mutex> lk(g_mutex);
            if (!g_blanking_active)
                return true;

            std::chrono::duration<double> elapsed =
                std::chrono::steady_clock::now() - g_blanking_started_at;
            elapsed_seconds = elapsed.count();
            animation_seconds = g_blanking_config.animation_seconds;
            portrait = (g_orientation == "portrait");
        }

        if (animation_seconds <= 0.0)
            animation_seconds = DEFAULT_BLANK_ANIMATION_SECONDS;

        double t = std::clamp(elapsed_seconds / animation_seconds, 0.0, 1.0);
        double eased = 1.0 - std::pow(1.0 - t, 3.0);
        double scale = 0.15 + 0.85 * eased;
        double tumble_rotation = (1.0 - eased) * 5.0 * PI;
        double orientation_rotation = portrait ? (-PI / 2.0) : 0.0;

        cr->save();
        cr->translate(w / 2.0, h / 2.0);
        // Settle into the display's orientation while tumbling into place.
        cr->rotate(orientation_rotation + tumble_rotation);
        cr->scale(scale, scale);

        if (g_separator_svg_handle && g_separator_svg_w > 0.0 && g_separator_svg_h > 0.0) {
            double target_w = std::min(static_cast<double>(portrait ? h : w) * 0.75, 1200.0);
            double fit = target_w / g_separator_svg_w;

            cr->scale(fit, fit);
            cr->translate(-g_separator_svg_w / 2.0, -g_separator_svg_h / 2.0);

            RsvgRectangle viewport = {0, 0, g_separator_svg_w, g_separator_svg_h};
            rsvg_handle_render_document(g_separator_svg_handle.get(), cr->cobj(), &viewport, nullptr);
        } else {
            cr->set_source_rgb(1.0, 0.0, 1.0);
            cr->set_line_width(6.0);
            const double half_w = std::min(static_cast<double>(w) * 0.28, 420.0);
            cr->move_to(-half_w, 0);
            cr->line_to(half_w, 0);
            cr->stroke();
        }

        cr->restore();
        return true;
    });
    root_overlay.add_overlay(blanking_area);
    root_overlay.set_overlay_pass_through(blanking_area, true);

    ScrollState* state = new ScrollState();
    state->content_box = content_box;
    g_state = state;

    window.signal_realize().connect([&]() {
        apply_orientation_layout();
        if (auto gdk_window = window.get_window())
            gdk_window->set_cursor(Gdk::Cursor::create(Gdk::BLANK_CURSOR));
        // Disable screensaver and DPMS so the display stays on
        system("xset s off s noblank -dpms 2>/dev/null");
    });

    scrolled.signal_size_allocate().connect([state, &scrolled](Gtk::Allocation&) {
        evaluate_scroll_state();
    });

    scrolled.add_tick_callback([state, viewport](const Glib::RefPtr<Gdk::FrameClock>& clock) {
        evaluate_scroll_state();
        update_blanking_animation();
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
