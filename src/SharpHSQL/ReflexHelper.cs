//===============================================================================
// SharpHsql
//
// ReflexHelper.cs
//
// This file contains the implementations of reflection utility methods.
//
//===============================================================================

using System;	
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Threading;
using System.Globalization;

namespace SharpHsql
{
	/// <summary>
	/// Provides utility methods for reflection-related operations.
	/// </summary>
	sealed class ReflexHelper
	{
		#if !POCKETPC
		/// <summary>
		/// Gets the Assembly Configuration Attribute.
		/// </summary>
		/// <param name="assembly">Assembly to get configuration.</param>
		/// <returns></returns>
		public static string GetAssemblyConfiguration(Assembly assembly) 
		{		
			object [] att = assembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
			return ((att.Length > 0) ? ((AssemblyConfigurationAttribute) att [0]).Configuration : String.Empty);
		}
		#endif		

		#if !POCKETPC
		/// <summary>
		/// Gets an assembly full path and file name.
		/// </summary>
		/// <param name="assembly">Assembly to get path.</param>
		/// <returns></returns>
		public static string GetAssemblyPath(Assembly assembly)
		{
			Uri uri = new Uri(assembly.CodeBase);
			return uri.LocalPath;
		}
		#endif

		#if !POCKETPC
		/// <summary>
		/// Gets the Assembly Title.
		/// </summary>
		/// <param name="assembly">Assembly to get title.</param>
		/// <returns></returns>
		public static string GetAssemblyTitle(Assembly assembly) 
		{		
			object [] att = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			return ((att.Length > 0) ? ((AssemblyTitleAttribute) att [0]).Title : String.Empty);
		}
		#endif

		#if !POCKETPC
		/// <summary>
		/// Instructs a compiler to use a specific version number for the Win32 file version resource. 
		/// The Win32 file version is not required to be the same as the assembly's version number.
		/// </summary>
		/// <param name="assembly">Assembly to get version.</param>
		/// <returns></returns>
		public static string GetAssemblyFileVersion(Assembly assembly) 
		{		
			object [] att = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
			return ((att.Length > 0) ? ((AssemblyFileVersionAttribute) att [0]).Version : String.Empty);
		}
		#endif

		#if !POCKETPC
		/// <summary>
		/// Gets the Assembly Product.
		/// </summary>
		/// <param name="assembly">Assembly to get product.</param>
		/// <returns></returns>
		public static string GetAssemblyProduct(Assembly assembly) 
		{		
			object [] att = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
			return ((att.Length > 0) ? ((AssemblyProductAttribute) att [0]).Product : String.Empty);
		}
		#endif
	}
}