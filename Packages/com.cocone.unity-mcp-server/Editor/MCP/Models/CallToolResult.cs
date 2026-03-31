using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityMcp.Models
{
    public class CallToolResult
    {
        [JsonProperty("content")]
        public List<object> Content { get; set; } = new List<object>();

        [JsonProperty("isError")]
        public bool IsError { get; set; }

        public static CallToolResult SuccessText(string text)
        {
            return new CallToolResult
            {
                Content = new List<object> { new TextContent { Text = text } },
                IsError = false
            };
        }

        public static CallToolResult ErrorText(string errorMessage)
        {
            return new CallToolResult
            {
                Content = new List<object> { new TextContent { Text = errorMessage } },
                IsError = true
            };
        }

        public static CallToolResult SuccessImage(string base64Data, string mimeType)
        {
            return new CallToolResult
            {
                Content = new List<object> { new ImageContent { Data = base64Data, MimeType = mimeType } },
                IsError = false
            };
        }
    }
}
