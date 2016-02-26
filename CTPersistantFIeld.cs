using System;
using System.Reflection;
namespace CameraTools
{
	[AttributeUsage(AttributeTargets.Field)]
	public class CTPersistantField : Attribute
	{
		public static string settingsURL = "GameData/CameraTools/settings.cfg";

		public CTPersistantField()
		{

		}

		public static void Save()
		{
			ConfigNode fileNode = ConfigNode.Load(settingsURL);
			ConfigNode settings = fileNode.GetNode("CToolsSettings");


			foreach(var field in typeof(CamTools).GetFields())
			{
				if(!field.IsDefined(typeof(CTPersistantField), false)) continue;

				settings.SetValue(field.Name, field.GetValue(CamTools.fetch).ToString(), true);
			}

			fileNode.Save(settingsURL);
		}

		public static void Load()
		{
			ConfigNode fileNode = ConfigNode.Load(settingsURL);
			ConfigNode settings = fileNode.GetNode("CToolsSettings");

			foreach(var field in typeof(CamTools).GetFields())
			{
				if(!field.IsDefined(typeof(CTPersistantField), false)) continue;

				if(settings.HasValue(field.Name))
				{
					object parsedValue = ParseValue(field.FieldType, settings.GetValue(field.Name));
					if(parsedValue != null)
					{
						field.SetValue(CamTools.fetch, parsedValue);
					}
				}
			}
		}

		public static object ParseValue(Type type, string value)
		{
			if(type == typeof(string))
			{
				return value;
			}

			if(type == typeof(bool))
			{
				return bool.Parse(value);
			}
			else if(type.IsEnum)
			{
				return Enum.Parse(type, value);
			}
			else if(type == typeof(float))
			{
				return float.Parse(value);
			}
			else if(type == typeof(Single))
			{
				return Single.Parse(value);
			}


			UnityEngine.Debug.LogError("CameraTools failed to parse settings field of type "+type.ToString()+" and value "+value);

			return null;
		}
	}
}

