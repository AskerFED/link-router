using System.Windows.Media;

namespace BrowserSelector
{
    public class BrowserInfoWithColor
    {
        public string Name { get; set; }
        public string ExecutablePath { get; set; }
        public string Type { get; set; }
        public string Color { get; set; }
        public ImageSource? Icon { get; set; }
    }
}