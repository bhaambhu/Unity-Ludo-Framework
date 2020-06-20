using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MobileConsole
{
	public class Style : MonoBehaviour
	{
		private MobileConsole.Skin _skin;

		public enum StyleType
		{
			LightBackground,
			DarkBackground,
			SelectedBackground,
			Text,
			SelectedText,
			ScrollbarBackground,
			ScrollbarHandle,
			ScrollBackground
		}

		[SerializeField] private StyleType _styleType;

		private Dictionary<MobileConsole.Skin, Dictionary<StyleType, Color>> Colors =
			new Dictionary<MobileConsole.Skin, Dictionary<StyleType, Color>>()
		{
			{MobileConsole.Skin.Light, new Dictionary<StyleType, Color>()
			{
				{StyleType.LightBackground, new Color(222 / 255.0f, 222 / 255.0f, 222 / 255.0f)},
				{StyleType.DarkBackground, new Color(216 / 255.0f, 216 / 255.0f, 216 / 255.0f)},
				{StyleType.SelectedBackground, new Color(61 / 255.0f, 125 / 255.0f, 231 / 255.0f)},
				{StyleType.Text, new Color(10 / 255.0f, 10 / 255.0f, 10 / 255.0f)},
				{StyleType.SelectedText, new Color(250 / 255.0f, 250 / 255.0f, 250 / 255.0f)},
				{StyleType.ScrollbarBackground, new Color(235 / 255.0f, 235 / 255.0f, 235 / 255.0f)},
				{StyleType.ScrollbarHandle, new Color(125 / 255.0f, 142 / 255.0f, 167 / 255.0f)},
				{StyleType.ScrollBackground, new Color(255 / 255.0f, 255 / 255.0f, 255 / 255.0f, 100 / 255.0f)}
			}},
			{MobileConsole.Skin.Dark, new Dictionary<StyleType, Color>()
			{
				{StyleType.LightBackground, new Color(60 / 255.0f, 60 / 255.0f, 60 / 255.0f)},
				{StyleType.DarkBackground, new Color(55 / 255.0f, 55 / 255.0f, 55 / 255.0f)},
				{StyleType.SelectedBackground, new Color(61 / 255.0f, 96 / 255.0f, 145 / 255.0f)},
				{StyleType.Text, new Color(180 / 255.0f, 180 / 255.0f, 180 / 255.0f)},
				{StyleType.SelectedText, new Color(250 / 255.0f, 250 / 255.0f, 250 / 255.0f)},
				{StyleType.ScrollbarBackground, new Color(50 / 255.0f, 50 / 255.0f, 50 / 255.0f)},
				{StyleType.ScrollbarHandle, new Color(90 / 255.0f, 90 / 255.0f, 90 / 255.0f)},
				{StyleType.ScrollBackground, new Color(255 / 255.0f, 255 / 255.0f, 255 / 255.0f, 5 / 255.0f)}
			}}
		};

		private Graphic _graphic;

		public void SetSkin(MobileConsole.Skin skin)
		{
			_skin = skin;
			Refresh();
		}
		
		public void SetStyle(StyleType styleType)
		{
			_styleType = styleType;
			Refresh();
		}

		private void Refresh()
		{
			if (_graphic == null)
			{
				AssignGraphic();
			}

			if (_graphic != null)
			{
				_graphic.color = Colors[_skin][_styleType];	
			}
		}

		private void AssignGraphic()
		{
			switch (_styleType)
			{
				case StyleType.Text:
				case StyleType.SelectedText:
					_graphic = GetComponent<Text>();
					break;
				default:
					_graphic = GetComponent<Image>();
					break;
			}
		}
	}
}
