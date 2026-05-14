// Resolve WPF/WinForms type ambiguities in favor of WPF
global using Application = System.Windows.Application;
global using MouseEventArgs = System.Windows.Input.MouseEventArgs;
global using Point = System.Windows.Point;
global using Color = System.Windows.Media.Color;
global using Pen = System.Windows.Media.Pen;
global using Brush = System.Windows.Media.Brush;
global using UserControl = System.Windows.Controls.UserControl;
global using Button = System.Windows.Controls.Button;
global using MessageBox = System.Windows.MessageBox;
global using Cursors = System.Windows.Input.Cursors;
global using ColorConverter = System.Windows.Media.ColorConverter;
global using FontFamily = System.Windows.Media.FontFamily;
global using HorizontalAlignment = System.Windows.HorizontalAlignment;
global using Thickness = System.Windows.Thickness;
