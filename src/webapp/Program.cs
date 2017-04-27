using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Nancy;
using Owin;

namespace webapp
{
	class Program
	{
		static void Main(string[] args)
		{
			const string url = "http://localhost:8001";
			using (WebApp.Start<Startup>(url))
			{
				Console.WriteLine("Running on {0}", url);
				Console.WriteLine("Press enter to exit");
				Console.ReadLine();
			}
		}
	}

	public class Module : NancyModule
	{
		public Module()
		{
			Get["/"] = _ => "<html><body>Hello World!</body></html>";
		}
	}

	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.UseNancy();
		}
	}
}
