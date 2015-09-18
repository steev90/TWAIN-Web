﻿using System.Collections.Generic;

namespace TwainWeb.Standalone.App
{    
    
    public class ScannerSettings
    {
	    private const float Backlash = 0.3f;
	    private readonly string _name;
        private readonly int? _id;
		private readonly List<float> _resolutions;
		private readonly Dictionary<int, string> _pixelTypes;
        private List<FormatPage> _allowedFormats;
		public ScannerSettings(int id, string name, List<float> resolutions = null, Dictionary<int, string> pixelTypes = null, float? maxHeight = null, float? maxWidth = null)
        {
            _name = name;
            _id = id;
            _resolutions = resolutions;
            _pixelTypes = pixelTypes;
            FillAllowedFormats(maxHeight, maxWidth);
        }

        private void reduceSize(ref float? maxWidth, ref float? maxHeight)
        {
            if (!maxHeight.HasValue || !maxWidth.HasValue)
                return;
            var buffer = maxHeight;
            maxHeight = maxWidth;
            maxWidth = buffer / 2;
        }

        private void FillAllowedFormats(float? maxHeight, float? maxWidth)
        {
	        if (!maxHeight.HasValue || !maxWidth.HasValue)
	        {
		        _allowedFormats = GlobalDictionaries.Formats;
		        return;
	        }
	        _allowedFormats = new List<FormatPage>();
            FormatPage prevFormat = null;
            bool? useStandartSizes = null;
            foreach(var format in GlobalDictionaries.Formats)
            {
                if (format.Height > maxHeight || format.Width > maxWidth)
                {
                    prevFormat = format; 
                    continue; 
                }

                if (useStandartSizes.HasValue)
                {
                    if (useStandartSizes.Value)
                        _allowedFormats.Add(format);
                    else
                    {
                        reduceSize(ref maxWidth, ref maxHeight);
                        _allowedFormats.Add(new FormatPage { Name = format.Name, Height = maxHeight.Value, Width = maxWidth.Value });
                    }
                }
                else
                {
                    if (format.EqualsWithBackslash(maxWidth.Value, maxHeight.Value, Backlash))
                    {
                        _allowedFormats.Add(new FormatPage { Name = format.Name, Height = maxHeight.Value, Width = maxWidth.Value });
                        useStandartSizes = false;                        
                    }
                    else if (prevFormat != null && prevFormat.EqualsWithBackslash(maxWidth.Value, maxHeight.Value, Backlash))
                    {
                        _allowedFormats.Add(new FormatPage { Name = prevFormat.Name, Height = maxHeight.Value, Width = maxWidth.Value });
                        reduceSize(ref maxWidth, ref maxHeight);
                        _allowedFormats.Add(new FormatPage { Name = format.Name, Height = maxHeight.Value, Width = maxWidth.Value });
                        useStandartSizes = false;
                    }
                    else
                    {
                        _allowedFormats.Add(new FormatPage { Name = "Максимальный размер", Height = maxHeight.Value, Width = maxWidth.Value });
                        _allowedFormats.Add(format);
                        useStandartSizes = true;
                    }                    
                }
            }
            if(_allowedFormats.Count == 0) _allowedFormats.Add(new FormatPage{Name="Максимальный размер", Width=maxWidth.Value, Height=maxHeight.Value});
        }

        public bool Compare(int id, string name)
        {
            return id == _id && _name == name;
        }

        public string Serialize()
        {
            var result = "";
            if (_resolutions != null && _resolutions.Count > 0)
                result += string.Format(",\"minResolution\": \"{0}\", \"maxResolution\": \"{1}\"", _resolutions[0], _resolutions[_resolutions.Count - 1]);

	        var iter = 0;
            if (_pixelTypes != null && _pixelTypes.Count > 0)
            {
                result += ",\"pixelTypes\": [";
	            foreach (var pixelType in _pixelTypes)
	            {
		            result += "{ \"key\": \"" + pixelType.Key + "\", \"value\": \"" +
                        pixelType.Value + "\"}"
						+ (iter != (_pixelTypes.Count - 1) ? "," : "");

					iter++;
	            }
	           
                result += "]";
            }
            if (_allowedFormats != null)
            {
                result += ",\"allowedFormats\": [";
                for (var i =0; i< _allowedFormats.Count; i++)
                {
                    result += "{\"key\": \"" + _allowedFormats[i].Width + "*" + _allowedFormats[i].Height + "\", \"value\":\"" + _allowedFormats[i].Name + "\"}"
                        + (i != (_allowedFormats.Count - 1) ? "," : "");
                }
                result += "]";
            }
            return result;
        }
    }
}