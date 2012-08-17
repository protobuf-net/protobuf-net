using System;
using System.IO;
using DAL;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
namespace Net11_Runner
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			MySerializer ser = new MySerializer();
			using(FileStream file = File.OpenRead("../../../Tools/nwind.proto.bin"))
			{
				DatabaseCompat db = (DatabaseCompat)ser.Deserialize(file, null, typeof(DatabaseCompat));
				Console.WriteLine("Orders: {0}", db.Orders.Count);
				int sum = 0;
				foreach(OrderCompat order in db.Orders)
				{
					sum += order.Lines == null ? 0 : order.Lines.Count;
				}
				Console.WriteLine("Lines: {0}", sum);

				using(MemoryStream ms = new MemoryStream())
				{
					ser.Serialize(ms, db);
					Console.WriteLine("Bytes: {0}", ms.Length);
				}
			}
			
//			ArrayList names = new ArrayList();
//			foreach(FieldInfo field in typeof(OpCodes).GetFields())
//			{
//				if(field.IsStatic) names.Add(field.Name);
//			}
//			names.Sort();
//			foreach(string name in names) Console.WriteLine(name);
		}
	}
}
