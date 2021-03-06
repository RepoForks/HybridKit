﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Java.Interop;

using Android.Webkit;
using Android.App;
using Android.OS;

namespace HybridKit.Android {

	sealed class WebViewScriptEvaluator : Java.Lang.Object, IScriptEvaluator {

		const string CallbackGlobalObject = "HybridKit_CallbackHandler";
		const string CallbackMethod = nameof(PostEvalResult);
		const string Callback = CallbackGlobalObject + "." + CallbackMethod;

		readonly object evalLock;
		readonly HybridWebView webView;
		readonly Activity dispatchActivity;
		TaskCompletionSource<string> resultTcs;

		public WebViewScriptEvaluator (HybridWebView webView)
		{
			this.evalLock = new object ();
			this.webView = webView;
			this.dispatchActivity = (Activity)webView.Context;
			webView.AddJavascriptInterface (this, CallbackGlobalObject);
		}

		internal void LoadHelperScript ()
		{
			using (var reader = new StreamReader (typeof (HybridKit).Assembly.GetManifestResourceStream ("HybridKit.HybridKit.js")))
				EvalNoResult (reader.ReadToEnd ());
		}

		void EvalNoResult (string script)
		{
			if (HybridWebView.IsJellybeanOrOlder) {
				var uri = "javascript:" + script;
				webView.LoadUrl (uri);
			} else {
				webView.EvaluateJavascript (script, null);
			}
		}

		public string Eval (string script)
		{
			lock (evalLock) {
				if (resultTcs != null)
					throw new InvalidOperationException ("Eval already in progress for this WebView.");
				if (webView.IsInWebClientFrame)
					throw new InvalidOperationException ("Cannot call Eval within WebViewClient callback.");

				var isMainThread = Looper.MyLooper () == Looper.MainLooper;
				if (isMainThread && !webView.CanRunScriptOnMainThread)
					throw new InvalidOperationException ("Cannot call Eval on the main thread in this Android version.");

				script = Callback + "(" + script + ")";
				resultTcs = new TaskCompletionSource<string> ();
				try {
					if (isMainThread)
						EvalNoResult (script);
					else
						dispatchActivity.RunOnUiThread (() => EvalNoResult (script));
					return resultTcs.Task.Result;
				} finally {
					resultTcs = null;
				}
			}
		}

		public void EvalOnMainThread (string script)
		{
			dispatchActivity.RunOnUiThread (() => EvalNoResult (script));
		}

		[Export, JavascriptInterface]
		public void PostEvalResult (string result)
		{
			resultTcs.TrySetResult (result);
		}
	}
}

