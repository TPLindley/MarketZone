#include <gtkmm.h>

int main(int argc, char* argv[])
{
    auto app = Gtk::Application::create(argc, argv, "com.terminal-solutions.szSpecials");

    Gtk::Window window;
    window.set_title("szSpecials");
    window.set_default_size(800, 600);

    return app->run(window);
}
