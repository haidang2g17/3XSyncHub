using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace _3XSyncHub
{
    /// Điểm khởi động chính của ứng dụng WPF 3XSyncHub
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            /// Tự động tìm & load DLL trong thư mục "Library" khi thiếu
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

#if DEBUG
            Console.WriteLine("[3XSyncHub] Program started (Debug Mode)");
#endif

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        /// Auto-load DLL từ thư mục Library khi WPF không tìm thấy
        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string libDir = Path.Combine(baseDir, "Library");

            string dllName = new AssemblyName(args.Name).Name + ".dll";
            string dllPath = Path.Combine(libDir, dllName);

            if (File.Exists(dllPath))
                return Assembly.LoadFrom(dllPath);

            return null;
        }
    }
}
