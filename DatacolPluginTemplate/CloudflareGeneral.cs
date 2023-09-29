using CefSharp;
using CefSharp.DevTools.Input;
using System.Drawing;
using CefSharp.WinForms;
using cloudfare_bypass_cefsharp;
using System;
using Point = System.Drawing.Point;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Threading;

namespace DatacolPluginTemplate.CloudflareConsigned
{


	public class LowercaseContractResolver : DefaultContractResolver
	{
		protected override string ResolvePropertyName(string propertyName)
		{
			var f = propertyName.Substring(0, 1).ToLower();
			string newstr = f + propertyName.Remove(0, 1);
			return newstr;
		}
	}


	public enum EMouseKey
	{
		// Token: 0x0400005B RID: 91
		LEFT,

		// Token: 0x0400005C RID: 92
		RIGHT,

		// Token: 0x0400005D RID: 93
		DOUBLE_LEFT,

		// Token: 0x0400005E RID: 94
		DOUBLE_RIGHT
	}

	public class ClickOptions
	{
		/// <summary>
		/// Time to wait between <c>mousedown</c> and <c>mouseup</c> in milliseconds. Defaults to 0
		/// </summary>
		public int Delay { get; set; } = 0;

		/// <summary>
		/// Defaults to 1. See https://developer.mozilla.org/en-US/docs/Web/API/UIEvent/detail
		/// </summary>
		public int ClickCount { get; set; } = 1;

		/// <summary>
		/// The button to use for the click. Defaults to <see cref="MouseButton.Left"/>
		/// </summary>
		public MouseButton Button { get; set; } = MouseButton.Left;
	}

	public class MoveOptions
	{
		/// <summary>
		/// Sends intermediate <c>mousemove</c> events. Defaults to 1
		/// </summary>
		public int Steps { get; set; } = 1;
	}

	[JsonConverter(typeof(StringEnumConverter))]
	internal enum MouseEventType
	{
		[EnumMember(Value = "mouseMoved")] MouseMoved,
		[EnumMember(Value = "mousePressed")] MousePressed,
		[EnumMember(Value = "mouseReleased")] MouseReleased,
		[EnumMember(Value = "mouseWheel")] MouseWheel
	}

	internal class InputDispatchMouseEventRequest
	{
		public MouseEventType Type { get; set; }

		public string Button { get; set; } = "left";

		public decimal X { get; set; }

		public decimal Y { get; set; }

		public int Modifiers { get; set; } = 0;

		public int ClickCount { get; set; }
	}
}
