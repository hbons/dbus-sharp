// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTest
{
	public static void Main (string[] args)
	{
		string addr = Address.SessionBus;

		if (args.Length == 1) {
			string arg = args[0];

			switch (arg)
			{
				case "--system":
					addr = Address.SystemBus;
					break;
				case "--session":
					addr = Address.SessionBus;
					break;
				default:
					Console.Error.WriteLine ("Usage: monitor.exe [--system | --session] [watch expressions]");
					Console.Error.WriteLine ("       If no watch expressions are provided, defaults will be used.");
					return;
			}
		}

		Connection conn = new Connection (false);
		conn.Open (addr);
		conn.Authenticate ();

		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		Bus bus = conn.GetObject<Bus> (name, opath);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		bus.Hello ();

		//hack to process the NameAcquired signal synchronously
		conn.HandleSignal (conn.ReadMessage ());

		if (args.Length > 1) {
			//install custom match rules only
			for (int i = 1 ; i != args.Length ; i++)
				bus.AddMatch (args[i]);
		} else {
			//no custom match rules, install the defaults
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Signal));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.MethodCall));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.MethodReturn));
			bus.AddMatch (MessageFilter.CreateMatchRule (MessageType.Error));
		}

		while (true) {
			Message msg = conn.ReadMessage ();
			PrintMessage (msg);
			Console.WriteLine ();
		}
	}

	public static void PrintMessage (Message msg)
	{
		Console.WriteLine ("Message:");
		Console.WriteLine ("\t" + "Type: " + msg.Header.MessageType);
		//foreach (HeaderField hf in msg.HeaderFields)
		//	Console.WriteLine ("\t" + hf.Code + ": " + hf.Value);
		foreach (KeyValuePair<FieldCode,object> field in msg.Header.Fields)
			Console.WriteLine ("\t" + field.Key + ": " + field.Value);

		if (msg.Body != null) {
			Console.WriteLine ("\tBody:");
			MessageReader reader = new MessageReader (msg);

			//TODO: this needs to be done more intelligently
			try {
				foreach (DType dtype in msg.Signature.Data) {
					if (dtype == DType.Invalid)
						continue;
					object arg;
					reader.GetValue (dtype, out arg);
					Console.WriteLine ("\t\t" + dtype + ": " + arg);
				}
			} catch {
				Console.WriteLine ("\t\tmonitor is too dumb to decode message body");
			}
		}
	}
}
