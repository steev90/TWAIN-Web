﻿namespace TwainWeb.Standalone.App.Models
{
	public class ActionResult
	{
		public byte[] Content { get; set; }
		public string ContentType { get; set; }
		public string FileNameToDownload { get; set; }
	}
}
