namespace viewers {
    using System;
    using System.Windows.Forms;

    static class Program {
        [STAThread]
        static void Main() {
            var client_id = Environment.GetEnvironmentVariable("TWITCH_CLIENTID");
            if (client_id == null) {
                throw new Exception("TWITCH_CLIENTID must be set");
            }
            var name = "museun"; // TODO make this configurable

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(name, client_id));
        }
    }
}
