using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsolvencniRejstrik
{
	public class UnknownPersonException : ApplicationException
	{
		public UnknownPersonException(string message) : base(message)
		{ }
	}

	public class UnknownRoleException : ApplicationException
	{
		public UnknownRoleException(string message) : base(message)
		{ }
	}
}
