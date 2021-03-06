﻿using AngleSharp.Dom;
using XamlCSS.Dom;
using Windows.UI.Xaml;

namespace XamlCSS.UWP.Dom
{
	public class NamedNodeMap : NamedNodeMapBase<DependencyObject, DependencyProperty>
	{
		public NamedNodeMap(DependencyObject dependencyObject)
			: base(dependencyObject)
		{

		}
		protected override IAttr CreateAttribute(DependencyObject dependencyObject, DependencyProperty property)
		{
			return new ElementAttribute(dependencyObject, property);
		}
	}
}
