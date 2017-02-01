using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.Core
{
   public static class BrowsableHelper
   {
      /// <summary>
      /// Set the Browsable property.
      /// NOTE: Be sure to decorate the property with [Browsable(true)]
      /// </summary>
      /// <param name="PropertyName">Name of the variable</param>
      /// <param name="bIsBrowsable">Browsable Value</param>
      public static void SetBrowsableProperty(this object obj, string strPropertyName, bool bIsBrowsable)
      {
         // Get the Descriptor's Properties
         PropertyDescriptor theDescriptor = TypeDescriptor.GetProperties(obj.GetType())[strPropertyName];

         // Get the Descriptor's "Browsable" Attribute
         BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)theDescriptor.Attributes[typeof(BrowsableAttribute)];
         FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);

         // Set the Descriptor's "Browsable" Attribute
         isBrowsable.SetValue(theDescriptorBrowsableAttribute, bIsBrowsable);
      }
   }
}
