using System;
using WandEnhancer.Core;

namespace WandEnhancer.View.MainWindow
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ELogType LogType { get; set; }
        public string Message { get; set; }

        public string TimeFormatted
        {
            get => Timestamp.ToString("HH:mm:ss");
            set { }
        }

        public string Tag
        {
            get
            {
                if (!string.IsNullOrEmpty(Message) && Message.StartsWith("[") && Message.IndexOf(']') > 1)
                {
                    int close = Message.IndexOf(']');
                    if (close <= 10)
                    {
                        return Message.Substring(1, close - 1).ToUpper();
                    }
                }

                switch (LogType)
                {
                    case ELogType.Error: return "ERROR";
                    case ELogType.Warn: return "WARN";
                    case ELogType.Success: return "SUCCESS";
                    case ELogType.Info: return "INFO";
                    default: return "LOG";
                }
            }
            set { }
        }

        public string TagColor
        {
            get
            {
                string tag = Tag;
                if (tag == "ERROR" || tag == "SECURITY" || tag == "ALERT") return "#f87171"; // Coral red
                if (tag == "WARN") return "#fbbf24"; // Amber
                if (tag == "SUCCESS") return "#4ade80"; // Bright green
                if (tag == "INFO" || tag == "TERMINAL") return "#38bdf8"; // Cyan
                if (tag == "AUTO" || tag == "PATCH" || tag == "DOWNLOAD" || tag == "INSTALL") return "#a78bfa"; // Purple
                return "#94a3b8"; // Slate
            }
            set { }
        }

        public string TagBackground
        {
            get
            {
                string tag = Tag;
                if (tag == "ERROR" || tag == "SECURITY" || tag == "ALERT") return "#3b1212";
                if (tag == "WARN") return "#3b2a0c";
                if (tag == "SUCCESS") return "#0d3319";
                if (tag == "INFO" || tag == "TERMINAL") return "#0c283d";
                if (tag == "AUTO" || tag == "PATCH" || tag == "DOWNLOAD" || tag == "INSTALL") return "#2e1065";
                return "#1e293b";
            }
            set { }
        }

        public string CleanMessage
        {
            get
            {
                if (string.IsNullOrEmpty(Message)) return string.Empty;
                
                string trimmed = Message.Trim();
                if (trimmed.StartsWith("[") && trimmed.IndexOf(']') > 0 && trimmed.IndexOf(']') < 20)
                {
                    return trimmed.Substring(trimmed.IndexOf(']') + 1).Trim();
                }
                return trimmed;
            }
            set { }
        }
    }
}
