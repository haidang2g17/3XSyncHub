using System.Threading.Tasks;
using System.Windows;
using _3XSyncHub.Views.Dialogs;

namespace _3XSyncHub.Services
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.Service.DialogService.cs
    // 🔖 Version: 20251119_1134
    // 📌 Nội dung đã xử lý:
    // - Cung cấp API hiển thị Dialog thống nhất (Info, Error, Success, Confirm, Danger)
    // - Dùng ClassicDialog với style chuẩn 3XVN.UI.Dialog
    // - Trả Task/Task<bool> để View gọi gọn, không cần try/catch
    // ============================================================================

    public static class DialogService
    {
        public static Task ShowInfo(string message, string title = "Thông Báo")
        {
            var dlg = new ClassicDialog(ClassicDialog.DialogType.Info, message, title)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
            return Task.CompletedTask;
        }

        public static Task ShowError(string message, string title = "Lỗi")
        {
            var dlg = new ClassicDialog(ClassicDialog.DialogType.Error, message, title)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
            return Task.CompletedTask;
        }

        public static Task ShowSuccess(string message, string title = "Thành công")
        {
            var dlg = new ClassicDialog(ClassicDialog.DialogType.Success, message, title)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
            return Task.CompletedTask;
        }

        public static Task<bool> ShowConfirm(string message, string title = "Xác nhận")
        {
            var dlg = new ClassicDialog(ClassicDialog.DialogType.Confirm, message, title)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
            return Task.FromResult(dlg.Result);
        }

        public static Task<bool> ShowDanger(string message, string title = "Cảnh báo")
        {
            var dlg = new ClassicDialog(ClassicDialog.DialogType.Danger, message, title)
            {
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();
            return Task.FromResult(dlg.Result);
        }
    }
}
