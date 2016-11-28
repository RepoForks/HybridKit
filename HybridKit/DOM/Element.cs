﻿using System;
using System.Threading.Tasks;

namespace HybridKit.DOM {

	public class Element : ScriptObject {

		protected Element (ScriptObject untyped): base (untyped)
		{
		}

		public string InnerText {
			set { this ["innerText"].SetValue (value); }
		}
		public Task<string> GetInnerText () => this ["innerText"].GetValue<string> ();

	}
}
