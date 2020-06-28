using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.IO;

class A1C{
		
	static BoundedChannelOptions one2one = new BoundedChannelOptions (1) {Capacity=1, SingleWriter=true, SingleReader=true,};

	enum MessageTag {North, West};

	struct Message { 
		public MessageTag Tag; 
		public int[] Val; 
	}; 

	static TaskCompletionSource<int> cspResult = new TaskCompletionSource<int>();
	static TaskCompletionSource<int> actResult = new TaskCompletionSource<int>();
	static TaskCompletionSource<int>[,] tempResult;

	static int diag(int k, int[] d0, int[] d1, int[] d2, string str1, string str2){
		int n1 = str1.Length;
		int n2 = str2.Length;
		int lo =  k < n2 ? 1 : k-n2+1;
		int hi = k < n1 ? k-1 : n1-1;
							
		for (int i = lo;i<=hi;i++){
			if(str1[i] == str2[k-i]){
				d2[i] = d0[i-1] + 1 ;
			}else{
				d2[i] = d1[i-1] < d1[i] ? d1[i] : d1[i-1];
			}
		}
		
		if (k < n1+n2-2){
			return diag(k+1, d1, d2, d0, str1, str2);
		} else {
			return d2[n1-1];
		}
	}

	static int seq(string str1, string str2) {
		str1 = "?" + str1;
		str2 = "?" + str2;
		int n1 = str1.Length;
				
		int[] d0 = new int[n1];
		int[] d1 = new int[n1];
		int[] d2 = new int[n1];
				
		return diag(2, d0, d1, d2, str1, str2);
	}

	static string[] split (string str, int div) {
		int n = str.Length;
		int q = n/div;
		int r = n%div;
		int [,] z = new int[div,2];
		string [] str1s = new string[div];
		for (int i = 0; i < div ;i++){
			if(r > 0){
				z[i,1] = q + 1;
				r--;
			}else{
				z[i,1] = q;
			}
		}
		for (int i = 1; i < div ;i++){
			z[i,0] = z[i-1,0] + z[i-1,1];
		}
		string strNew = "?"+str;
		for (int i = 0; i < div ;i++){
			str1s[i] = strNew.Substring (z[i,0], z[i,1]+1);
		}
		return str1s;
	}

	static int diag2(int k, int[] d0, int[] d1, int[] d2, string str1, string str2, int[] n, int[] w){
		int rows = str1.Length;
		int cols = str2.Length;
		
		int lo =  k < cols ? 1 : k-cols+1;
		int hi = k < rows ? k-1 : rows-1;
				  
		if (k < cols) { d2[0] = n[k]; }
		if (k < rows) { d2[k] = w[k]; }
				
		for (int i = lo;i<=hi;i++){
			if(str1[i] == str2[k-i]){
				d2[i] = d0[i-1] + 1 ;
			}else{
				d2[i] = d1[i-1] < d1[i] ? d1[i] : d1[i-1];
			}
		}
		if (k >= rows-1) { n[k-rows+1] = d2[rows-1];}
		if (k >= cols-1) { w[k-cols+1] = d2[k-cols+1];}
			
		if (k < cols+rows-2){
			return diag2(k+1, d1, d2, d0, str1, str2, n, w);
		} else {
			return d2[rows-1];
		}
	}

	static void actor (int i1, int i2, string str1, string str2, Channel<Message>[,] channels) { 

		Task.Run(async () => {
			int b1 = channels.GetLength(0);
			int b2 = channels.GetLength(1);
			Message msg;
			int[] n = new int [b2];
			int[] w = new int [b1];
		
			msg = await channels[i1,i2].Reader.ReadAsync ();
			switch (msg.Tag) {
				case MessageTag.North:
					n = msg.Val;
					break;
				case MessageTag.West:
					w = msg.Val;
					break;
			}
		
			msg = await channels[i1,i2].Reader.ReadAsync ();
			switch (msg.Tag) {
				case MessageTag.North:
					n = msg.Val;
					break;
				case MessageTag.West:
					w = msg.Val;
					break;
			}
		
			int rows = str1.Length;
			int cols = str2.Length;
		
			int[] d0 = new int[rows];
			int[] d1 = new int[rows];
			int[] d2 = new int[rows];        
		
			d0[0] = n[0];
			d1[0] = n[1];
			d1[1] = w[1];
		
			int r = diag2(2, d0, d1, d2, str1, str2, n, w);
		
			tempResult[i1,i2].SetResult(r);
		
			if (i1 == (b1-1) && i2 == (b2-1)) { 
				actResult.SetResult (r);
			} else { 
				if (i1 < b1-1) { 
					await channels[i1+1,i2].Writer.WriteAsync (new Message {Tag=MessageTag.North, Val=n});
				} 
				if (i2 < b2-1) { 
					await channels[i1,i2+1].Writer.WriteAsync (new Message {Tag=MessageTag.West, Val=w});
				}
			}
		});
	}

	static int act(string str1, string str2, int b1, int b2) {
		Channel<Message>[,] channels = new Channel<Message>[b1,b2];
		tempResult = new TaskCompletionSource<int>[b1,b2];
		
		string[] str1s = split(str1,b1);
		string[] str2s = split(str2,b2);

		for (int i1 = 0; i1 < b1; i1++ ){
			for (int i2 = 0; i2 < b2; i2++ ){
				channels[i1,i2] = Channel.CreateUnbounded<Message> ();
				tempResult[i1,i2] = new TaskCompletionSource<int> ();
				actor(i1, i2, str1s[i1], str2s[i2],channels);
			}
		}

		Task.Run(async () => {
			for (int i2 = 0; i2 < b2; i2++ ){
				await channels[0, i2].Writer.WriteAsync (new Message {Tag=MessageTag.North, Val=new int[str2s[i2].Length]});
			}
		
			for (int i1 = 0; i1 < b1; i1++ ){
				await channels[i1, 0].Writer.WriteAsync (new Message {Tag=MessageTag.West, Val=new int[str1s[i1].Length]});
			}
		});
		return actResult.Task.Result;
	}

	static async Task agent (int i1, int i2, string str1, string str2, Channel<Message>[,] channels) { 

		int b1 = channels.GetLength(0);
		int b2 = channels.GetLength(1);
		Message msg;
		int[] n = new int [b2];
		int[] w = new int [b1];
		
		msg = await channels[i1,i2].Reader.ReadAsync ();
		switch (msg.Tag) {
			case MessageTag.North:
				n = msg.Val;
				break;
			case MessageTag.West:
				w = msg.Val;
				break;
		}
		
		msg = await channels[i1,i2].Reader.ReadAsync ();
		switch (msg.Tag) {
			case MessageTag.North:
				n = msg.Val;
				break;
			case MessageTag.West:
				w = msg.Val;
				break;
		}

		int rows = str1.Length;
		int cols = str2.Length;

		int[] d0 = new int[rows];
		int[] d1 = new int[rows];
		int[] d2 = new int[rows];        

		d0[0] = n[0];
		d1[0] = n[1];
		d1[1] = w[1];

		int r = diag2(2, d0, d1, d2, str1, str2, n, w);

		tempResult[i1,i2].SetResult(r);

		if (i1 == (b1-1) && i2 == (b2-1)) { 
			cspResult.SetResult (r);
		} else { 
			if (i1 < b1-1) { 
				await channels[i1+1,i2].Writer.WriteAsync (new Message {Tag=MessageTag.North, Val=n});
			} 
			if (i2 < b2-1) { 
				await channels[i1,i2+1].Writer.WriteAsync (new Message {Tag=MessageTag.West, Val=w});
			}
		}
	}

	static async Task setup(Channel<Message>[,] channels,string[] str1s, string[] str2s) {
		int b1 = channels.GetLength(0);
		int b2 = channels.GetLength(1);
		for (int i2 = 0; i2 < b2; i2++ ){
			await channels[0, i2].Writer.WriteAsync (new Message {Tag=MessageTag.North, Val=new int[str2s[i2].Length]});
		}

		for (int i1 = 0; i1 < b1; i1++ ){
			await channels[i1, 0].Writer.WriteAsync (new Message {Tag=MessageTag.West, Val=new int[str1s[i1].Length]});
		}
	}

	static int csp(string str1, string str2, int b1, int b2) {
		Channel<Message>[,] channels = new Channel<Message>[b1,b2];
		tempResult = new TaskCompletionSource<int>[b1,b2];
		
		string[] str1s = split(str1,b1);
		string[] str2s = split(str2,b2);

		for (int i1 = 0; i1 < b1; i1++ ){
			for (int i2 = 0; i2 < b2; i2++ ){
				channels[i1,i2] = Channel.CreateBounded<Message> (one2one);
				tempResult[i1,i2] = new TaskCompletionSource<int> ();
				var a = agent(i1, i2, str1s[i1], str2s[i2],channels);
			}
		}

		var s = setup(channels,str1s,str2s);
		return cspResult.Task.Result;
	}

	static void Main(string[] args){
		string path1 = args[0].Substring(4);
		string  path2 = args[1].Substring(4);
		string str1 = File.ReadAllText(path1);
		string str2 = File.ReadAllText(path2);
		if (String.Equals(args[2], "/SEQ")) {
			Console.WriteLine("1 1 {0}", seq(str1,str2));
		} else {
			if (String.Equals(args[2].Substring(0,4), "/ACT")){
				string[] a = args[2].Substring(5).Split(',');
				if (int.Parse(a[2]) == 0) { Console.WriteLine("{0} {1} {2}",int.Parse(a[0])-1, int.Parse(a[1])-1, act(str1,str2,int.Parse(a[0]), int.Parse(a[1])));}
				else if(int.Parse(a[2]) == 1){
					act(str1,str2,int.Parse(a[0]), int.Parse(a[1]));
					for (int i1 = 0; i1 < int.Parse(a[0]); i1++ ){
						for (int i2 = 0; i2 < int.Parse(a[1]); i2++ ){
							Console.WriteLine("{0} {1} {2}", i1, i2, tempResult[i1,i2].Task.Result);
						}
					}
				}
			} else if(String.Equals(args[2].Substring(0,4), "/CSP")){
				string[] a = args[2].Substring(5).Split(',');
				if (int.Parse(a[2]) == 0) { Console.WriteLine("{0} {1} {2}",int.Parse(a[0])-1, int.Parse(a[1])-1, csp(str1,str2,int.Parse(a[0]), int.Parse(a[1])));}
				else if(int.Parse(a[2]) == 1){
					csp(str1,str2,int.Parse(a[0]), int.Parse(a[1]));
					for (int i1 = 0; i1 < int.Parse(a[0]); i1++ ){
						for (int i2 = 0; i2 < int.Parse(a[1]); i2++ ){
							Console.WriteLine("{0} {1} {2}", i1, i2, tempResult[i1,i2].Task.Result);
						}
					}
				}
			}
		}
	}
}