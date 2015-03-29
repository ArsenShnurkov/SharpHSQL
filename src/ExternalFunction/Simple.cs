using System;

namespace ExternalFunction
{
	/// <summary>
	/// Summary description for Simple.
	/// </summary>
	public class Simple
	{
		public Simple()
		{
		}

		public decimal calcrate( decimal amount, decimal percent )
		{
			return amount + ( amount * percent / 100 );
		}

		public static double tan( double value )
		{
			return Math.Tan( value );
		}
	}
}
