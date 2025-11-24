using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace _3XSyncHub.Views.Dialogs
{
    // ============================================================================
    // 🗂️ Module: 3XSyncHub.UI.ClassicDialog.xaml.cs
    // 🔖 Version: 20251119_1202
    // 📌 Nội dung đã xử lý:
    // - Triển khai Dialog đa dạng: Info / Error / Success / Confirm / Danger
    // - Tự động chọn icon + màu tiêu đề theo loại dialog
    // - Thêm button động (Primary / Secondary) theo từng trường hợp
    // - Fade-in animation mượt tuyệt đối qua Loaded event (Opacity 0 → 1)
    // ============================================================================

    public partial class ClassicDialog : Window
    {
        public enum DialogType { Info, Error, Success, Confirm, Danger }

        public bool Result { get; private set; }

        private static readonly SolidColorBrush BrushInfo =
            new((Color)ColorConverter.ConvertFromString("#2E3A59"));
        private static readonly SolidColorBrush BrushError =
            new((Color)ColorConverter.ConvertFromString("#C0392B"));
        private static readonly SolidColorBrush BrushSuccess =
            new((Color)ColorConverter.ConvertFromString("#2E7D32"));

        public ClassicDialog(DialogType type, string message, string title = "")
        {
            InitializeComponent();

            /// ✅ Ẩn bằng opacity (chứ không ẩn bằng Visibility)
            Opacity = 0;
            Title = "3X SyncHub";
            txtMessage.Text = message;

            switch (type)
            {
                case DialogType.Info:
                    SetDialog("ℹ️", title, "Thông Báo", BrushInfo);
                    AddButton("Đóng", PrimaryButton_Click, true);
                    break;

                case DialogType.Error:
                    SetDialog("❌", title, "Lỗi", BrushError);
                    AddButton("Đóng", PrimaryButton_Click, true);
                    break;

                case DialogType.Success:
                    SetDialog("✅", title, "Thành công", BrushSuccess);
                    AddButton("Đóng", PrimaryButton_Click, true);
                    break;

                case DialogType.Confirm:
                    SetDialog("⚙️", title, "Xác nhận", BrushInfo);
                    AddButton("Có", PrimaryButton_Click, true);
                    AddButton("Không", SecondaryButton_Click, false);
                    break;

                case DialogType.Danger:
                    SetDialog("🧨", title, "Cảnh báo", BrushError);
                    AddButton("Xóa", PrimaryButton_Click, true);
                    AddButton("Hủy", SecondaryButton_Click, false);
                    break;
            }

            /// ✅ Gắn event Loaded để fade mượt khi form thực sự được render
            Loaded += (s, e) => FadeIn();
        }

        private void SetDialog(string icon, string title, string defaultTitle, Brush color)
        {
            txtIcon.Text = icon;
            txtTitle.Text = string.IsNullOrWhiteSpace(title) ? defaultTitle : title;
            txtTitle.Foreground = color;
        }

        private void AddButton(string content, RoutedEventHandler click, bool isPrimary)
        {
            var btn = new Button
            {
                Content = content,
                Width = 100,
                Style = (Style)Application.Current.Resources[
                    isPrimary ? "PrimaryButtonStyle" : "SecondaryButtonStyle"]
            };
            btn.Click += click;
            ButtonPanel?.Children.Add(btn);
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        /// ✅ Fade-in animation mượt tuyệt đối, không nháy, không mất form
        private void FadeIn()
        {
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            {
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.8
            };
            BeginAnimation(OpacityProperty, fade);
        }
    }
}
