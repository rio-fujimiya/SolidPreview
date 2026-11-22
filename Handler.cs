using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

using Microsoft.Win32;

[assembly: ComVisible(false)]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
namespace SolidPreview {
 [ComVisible(true), Guid("B824B49D-22AC-4161-AC8A-9916E8FA3F7F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 public interface IInitializeWithStream { [PreserveSig] int Initialize(IStream stream, uint mode); }
 [ComVisible(true), Guid("E357FCCD-A995-4576-B01F-234630154E96"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 public interface IThumbnailProvider { [PreserveSig] int GetThumbnail(uint cx, out IntPtr bitmap, out uint alpha); }
 [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
 [StructLayout(LayoutKind.Sequential)] public struct MSG { public IntPtr hwnd; public uint message; public UIntPtr wParam; public IntPtr lParam; public uint time; public int x,y; public uint lPrivate; }
 [ComVisible(true), Guid("8895B1C6-B41F-4C1C-A562-0D564250836F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 public interface IPreviewHandler {
  [PreserveSig] int SetWindow(IntPtr hwnd, ref RECT rect);
  [PreserveSig] int SetRect(ref RECT rect);
  [PreserveSig] int DoPreview(); [PreserveSig] int Unload(); [PreserveSig] int SetFocus();
  [PreserveSig] int QueryFocus(out IntPtr hwnd); [PreserveSig] int TranslateAccelerator(ref MSG msg);
 }
 [ComVisible(true), Guid("00000114-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 public interface IOleWindow { [PreserveSig] int GetWindow(out IntPtr hwnd); [PreserveSig] int ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool enter); }
 [ComVisible(true), Guid("FC4801A3-2BA9-11CF-A229-00AA003D7352"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 public interface IObjectWithSite { [PreserveSig] int SetSite([MarshalAs(UnmanagedType.IUnknown)] object site); [PreserveSig] int GetSite(ref Guid iid, out IntPtr site); }
 [ComImport, Guid("FEC87AAF-35F9-447A-ADB7-20234491401A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface IPreviewHandlerFrame { [PreserveSig] int GetWindowContext(IntPtr info); [PreserveSig] int TranslateAccelerator(ref MSG msg); }
 static class Native {
  [DllImport("shell32.dll")] static extern int SHGetKnownFolderPath(ref Guid id,uint flags,IntPtr token,out IntPtr path);
  internal static string Cache { get { Guid g=new Guid("A520A1A4-1780-4FF6-BD18-167343C5AF16"); IntPtr p; Marshal.ThrowExceptionForHR(SHGetKnownFolderPath(ref g,0,IntPtr.Zero,out p)); try { return Path.Combine(Marshal.PtrToStringUni(p),"SolidPreview","cache-v1"); } finally { Marshal.FreeCoTaskMem(p); } } }
  [DllImport("user32.dll", SetLastError=true)] internal static extern IntPtr SetParent(IntPtr child,IntPtr parent);
  [DllImport("user32.dll")] internal static extern IntPtr GetFocus();
  [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern IntPtr CreateWindowEx(int exStyle,string cls,string title,int style,int x,int y,int w,int h,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr param);
  [DllImport("user32.dll", SetLastError=true)] internal static extern bool DestroyWindow(IntPtr hwnd);
  [DllImport("user32.dll", SetLastError=true)] internal static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int w,int h,uint flags);
  [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr hwnd,uint msg,IntPtr w,IntPtr l);
  [DllImport("user32.dll")] internal static extern IntPtr SetFocus(IntPtr hwnd);
  [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr hwnd);
  [DllImport("user32.dll")] internal static extern bool InvalidateRect(IntPtr hwnd,IntPtr rect,bool erase);
  [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
  [StructLayout(LayoutKind.Sequential)] internal struct BITMAPINFOHEADER {
   public uint Size; public int Width,Height; public ushort Planes,BitCount; public uint Compression,SizeImage; public int XPelsPerMeter,YPelsPerMeter; public uint ClrUsed,ClrImportant;
  }
  [DllImport("gdi32.dll",SetLastError=true)] static extern IntPtr CreateDIBSection(IntPtr dc,ref BITMAPINFOHEADER info,uint usage,out IntPtr bits,IntPtr section,uint offset);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] internal static extern bool MoveFileEx(string source,string destination,uint flags);
  internal static IntPtr MakeDib(Bitmap b) {
   var info=new BITMAPINFOHEADER{Size=40,Width=b.Width,Height=-b.Height,Planes=1,BitCount=32};
   IntPtr bits; IntPtr h=CreateDIBSection(IntPtr.Zero,ref info,0,out bits,IntPtr.Zero,0);
   if(h==IntPtr.Zero || bits==IntPtr.Zero){if(h!=IntPtr.Zero)DeleteObject(h);throw new IOException("CreateDIBSection failed: "+Marshal.GetLastWin32Error());}
   try {
    var data=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppRgb);
    try { int stride=checked(b.Width*4); byte[] row=new byte[stride]; for(int y=0;y<b.Height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,stride);Marshal.Copy(row,0,IntPtr.Add(bits,y*stride),stride);} }
    finally {b.UnlockBits(data);}
    return h;
   } catch {DeleteObject(h);throw;}
  }
  [UnmanagedFunctionPointer(CallingConvention.Winapi)] internal delegate IntPtr SubclassProc(IntPtr h,uint msg,IntPtr w,IntPtr l,UIntPtr id,UIntPtr data);
  [DllImport("comctl32.dll",SetLastError=true)] internal static extern bool SetWindowSubclass(IntPtr h,SubclassProc proc,UIntPtr id,UIntPtr data);
  [DllImport("comctl32.dll")] internal static extern bool RemoveWindowSubclass(IntPtr h,SubclassProc proc,UIntPtr id);
  [DllImport("comctl32.dll")] internal static extern IntPtr DefSubclassProc(IntPtr h,uint msg,IntPtr w,IntPtr l);
  [StructLayout(LayoutKind.Sequential)] internal struct TRACKMOUSEEVENT { internal uint Size,Flags; internal IntPtr Hwnd; internal uint HoverTime; }
  [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT t);
  [DllImport("user32.dll")] internal static extern IntPtr SetCapture(IntPtr h);
  [DllImport("user32.dll")] internal static extern IntPtr GetCapture();
  [DllImport("user32.dll")] internal static extern bool ReleaseCapture();
  [DllImport("user32.dll", EntryPoint="PeekMessageW")] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool PeekMessage(out MSG msg,IntPtr hwnd,uint min,uint max,uint remove);
  [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool TranslateMessage(ref MSG msg);
  [DllImport("user32.dll", EntryPoint="DispatchMessageW")] internal static extern IntPtr DispatchMessage(ref MSG msg);
  [DllImport("user32.dll")] internal static extern uint MsgWaitForMultipleObjectsEx(uint count,IntPtr handles,uint milliseconds,uint wakeMask,uint flags);
  static readonly object logGate=new object();
  internal static void Log(string msg) {try {lock(logGate) {string root=Cache; Directory.CreateDirectory(root); string path=Path.Combine(root,"host-"+Process.GetCurrentProcess().Id+".log"); if(File.Exists(path)&&new FileInfo(path).Length>1024*1024) File.Delete(path); File.AppendAllText(path,DateTime.Now.ToString("HH:mm:ss.fff")+" T"+Thread.CurrentThread.ManagedThreadId+" "+msg+Environment.NewLine);}}catch{}}

 }
 // Shared with the installer so the same camera contract is tested and used at runtime.
 public sealed class CameraFrame {
  readonly double[] lo={Double.PositiveInfinity,Double.PositiveInfinity,Double.PositiveInfinity};
  readonly double[] hi={Double.NegativeInfinity,Double.NegativeInfinity,Double.NegativeInfinity};
  int count;
  static bool Finite(double n){return !Double.IsNaN(n)&&!Double.IsInfinity(n);}
  static double[] Vector(object value){
   var list=value as IList;if(list==null||list.Count!=3)return null;
   double[] v=new double[3];
   for(int i=0;i<3;i++){
    object x=list[i];if(x==null||x is string||x is bool)return null;
    try{v[i]=Convert.ToDouble(x,CultureInfo.InvariantCulture);}catch{return null;}
    if(!Finite(v[i]))return null;
   }return v;
  }
  void Scan(object value,int depth){
   if(depth>64||value==null)return;
   var map=value as IDictionary<string,object>;
   if(map!=null){
    foreach(var item in map){
     if(item.Key=="bbox"){
      var box=item.Value as IDictionary<string,object>;object a,b;
      if(box!=null&&box.TryGetValue("min",out a)&&box.TryGetValue("max",out b)){
       var x=Vector(a);var y=Vector(b);
       if(x!=null&&y!=null&&x[0]<=y[0]&&x[1]<=y[1]&&x[2]<=y[2]){
        for(int i=0;i<3;i++){lo[i]=Math.Min(lo[i],x[i]);hi[i]=Math.Max(hi[i],y[i]);}count++;
       }
      }
     }else Scan(item.Value,depth+1);
    }
   }else{var list=value as IList;if(list!=null)foreach(object child in list)Scan(child,depth+1);}
  }
  public static CameraFrame Parse(string json){
   if(String.IsNullOrWhiteSpace(json)||json.Length>8*1024*1024)throw new IOException("Invalid eval JSON size");
   var frame=new CameraFrame();
   var serializer=new JavaScriptSerializer{MaxJsonLength=8*1024*1024,RecursionLimit=128};
   frame.Scan(serializer.DeserializeObject(json),0);
   if(frame.count==0)throw new IOException("SolidCraft eval returned no valid bbox");
   frame.ToJson(0,0); // Validate the frame even before rendering.
   return frame;
  }
  public string ToJson(double yaw,double pitch){return ToJson(yaw,pitch,1.0);}
  public string ToJson(double yaw,double pitch,double aspect){return ToJson(yaw,pitch,aspect,1.0,0,0,0);}
  // Pan is expressed in world-space fractions of the model bounding diagonal.
  public string ToJson(double yaw,double pitch,double aspect,double zoom,double panX,double panY,double panZ){
   double x=(lo[0]+hi[0])*0.5,y=(lo[1]+hi[1])*0.5,z=(lo[2]+hi[2])*0.5;
   double dx=hi[0]-lo[0],dy=hi[1]-lo[1],dz=hi[2]-lo[2];
   double diagonal=Math.Sqrt(dx*dx+dy*dy+dz*dz);
   // Orthographic half-height = 0.4 * distance. Add margin around the bounding sphere.
   double dist=Math.Max(0.001,diagonal*CameraNavigation.DistanceScale(aspect)/CameraNavigation.ClampZoom(zoom));
   x+=diagonal*panX;y+=diagonal*panY;z+=diagonal*panZ;
   if(!Finite(x)||!Finite(y)||!Finite(z)||!Finite(dist)||!Finite(yaw)||!Finite(pitch)||diagonal<=0)throw new IOException("Invalid model bounds or camera");
   return String.Format(CultureInfo.InvariantCulture,"{{\"target\":[{0:R},{1:R},{2:R}],\"distance\":{3:R},\"yaw\":{4:R},\"pitch\":{5:R},\"fov\":0}}",x,y,z,dist,yaw,pitch);
  }
 }
 // Pure camera math, shared with the no-CLI Windows CI tests.
 public static class CameraNavigation {
  public static double ClampZoom(double zoom){return Math.Max(0.2,Math.Min(12.0,zoom));}
  public static double DistanceScale(double aspect){return 1.65/Math.Min(1.0,Math.Max(0.01,aspect));}
  public static double WheelZoom(double zoom,int delta){return ClampZoom(zoom*Math.Pow(1.2,delta/120.0));}
  // In orthographic projection the vertical world span is 0.8 * camera distance.
  // Drag right/down moves the model right/down (target moves left/up).
  public static double[] PanDelta(double yaw,double pitch,double zoom,int width,int height,int dx,int dy){
   double a=yaw*Math.PI/180.0,b=pitch*Math.PI/180.0;
   double unit=0.8*DistanceScale((double)Math.Max(1,width)/Math.Max(1,height))/(ClampZoom(zoom)*Math.Max(1,height));
   return new[]{unit*(-dx*Math.Cos(a)+dy*Math.Sin(a)*Math.Sin(b)),
     unit*(dx*Math.Sin(a)+dy*Math.Cos(a)*Math.Sin(b)),unit*dy*Math.Cos(b)};
  }
 }
 // A private headless MCP session, never connected to the desktop application.
 // All requests run on a single render worker; the UI/COM apartments do not wait.
 sealed class LiveSession : IDisposable {
  readonly Process proc; readonly StreamWriter input; readonly StreamReader output;
  readonly string work; int id; bool disposed;
  readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=48*1024*1024,RecursionLimit=128};
  static IDictionary<string,object> Map(object o){return o as IDictionary<string,object>;}
  static object Get(IDictionary<string,object> m,string k){object v;return m!=null&&m.TryGetValue(k,out v)?v:null;}
  static void Wait(Task t,CancellationToken ct,Stopwatch sw,int budget){
   while(!t.IsCompleted){ct.ThrowIfCancellationRequested();if(sw.ElapsedMilliseconds>=budget)throw new TimeoutException("Live MCP timeout");Thread.Sleep(10);}
   ct.ThrowIfCancellationRequested();t.GetAwaiter().GetResult();
  }
  internal LiveSession(string cli,string model,string work,CancellationToken ct){
   this.work=work;
   var psi=new ProcessStartInfo(cli,"mcp --headless --in "+Render.Quote(model)+" --allow-path "+Render.Quote(work)){
    UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=work,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,
    StandardOutputEncoding=new UTF8Encoding(false),StandardErrorEncoding=new UTF8Encoding(false)};
   psi.EnvironmentVariables["TEMP"]=work;psi.EnvironmentVariables["TMP"]=work;psi.EnvironmentVariables["SOLIDCRAFT_GPU"]="auto";
   proc=new Process{StartInfo=psi};
   try{
    ct.ThrowIfCancellationRequested();proc.ErrorDataReceived+=(sender,e)=>{if(e.Data!=null&&e.Data.IndexOf("error",StringComparison.OrdinalIgnoreCase)>=0)Native.Log("Live MCP stderr: "+e.Data.Substring(0,Math.Min(240,e.Data.Length)));};
    proc.Start();proc.BeginErrorReadLine();input=new StreamWriter(proc.StandardInput.BaseStream,new UTF8Encoding(false));output=proc.StandardOutput;
    var sw=Stopwatch.StartNew();
    var result=Map(Request("initialize",new {protocolVersion="2025-06-18",capabilities=new object(),clientInfo=new {name="SolidPreview",version="1.0.18"}},ct,sw,20000));
    if(Get(result,"protocolVersion")==null)throw new IOException("MCP initialize did not return a protocol version");
    Write(new {jsonrpc="2.0",method="notifications/initialized"},ct,sw,20000);
    Native.Log("Live MCP ready");
   }catch{Dispose();throw;}
  }
  void Write(object value,CancellationToken ct,Stopwatch sw,int budget){
   Wait(input.WriteLineAsync(json.Serialize(value)),ct,sw,budget);Wait(input.FlushAsync(),ct,sw,budget);
  }
  object Request(string method,object args,CancellationToken ct,Stopwatch sw,int budget){
   int requestId=++id;
   Write(new Dictionary<string,object>{{"jsonrpc","2.0"},{"id",requestId},{"method",method},{"params",args}},ct,sw,budget);
   while(true){
    ct.ThrowIfCancellationRequested();if(sw.ElapsedMilliseconds>=budget)throw new TimeoutException("Live MCP response timeout");
    var read=output.ReadLineAsync();Wait(read,ct,sw,budget);string line=read.Result;
    if(line==null)throw new IOException("Live MCP exited");if(line.Length>48*1024*1024)throw new IOException("Live MCP response too large");
    var response=Map(json.DeserializeObject(line));object rid=Get(response,"id");
    if(rid==null||Convert.ToString(rid,CultureInfo.InvariantCulture)!=requestId.ToString(CultureInfo.InvariantCulture))continue;
    if(Get(response,"error")!=null)throw new IOException("Live MCP RPC failed: "+json.Serialize(Get(response,"error")));
    return Get(response,"result");
   }
  }
  IList Tool(string name,object args,CancellationToken ct,int budget){
   var res=Map(Request("tools/call",new {name=name,arguments=args},ct,Stopwatch.StartNew(),budget));
   var content=Get(res,"content") as IList;
   if(Object.Equals(Get(res,"isError"),true)||content==null){string message="";if(content!=null&&content.Count>0)message=Get(Map(content[0]),"text") as string??"";throw new IOException("Live MCP "+name+" failed: "+message.Substring(0,Math.Min(400,message.Length)));}
   return content;
  }
  internal CameraFrame Measure(CancellationToken ct){
   var items=Tool("measure",new object(),ct,18000);
   foreach(object item in items){string text=Get(Map(item),"text") as string;if(!String.IsNullOrEmpty(text)){try{return CameraFrame.Parse(text);}catch{}}}
   throw new IOException("Live MCP measure returned no usable bounds");
  }
  internal string Screenshot(string camera,int width,int height,CancellationToken ct){
   string path=Path.Combine(work,"live-frame.png");if(File.Exists(path))File.Delete(path);
   var args=new Dictionary<string,object>{{"camera",json.DeserializeObject(camera)},{"width",width},{"height",height},{"grid",false},{"axes",false},{"source","model"},{"path",path}};
   var sw=Stopwatch.StartNew();var items=Tool("screenshot",args,ct,18000);
   // Some compatible releases return image content without writing path.
   if(!Render.IsPng(path))foreach(object item in items){var m=Map(item);if(Object.Equals(Get(m,"type"),"image")&&Object.Equals(Get(m,"mimeType"),"image/png")){
    string data=Get(m,"data") as string;if(data==null||data.Length>44*1024*1024)throw new IOException("Live MCP PNG too large");File.WriteAllBytes(path,Convert.FromBase64String(data));break;
   }}
   if(!Render.IsPng(path))throw new IOException("Live MCP returned no valid PNG");
   if(!Render.IsPng(path,width,height))throw new IOException("Live MCP screenshot size mismatch");
   Native.Log("Live frame "+width+"x"+height+" "+sw.ElapsedMilliseconds+"ms");return path;
  }
  public void Dispose(){
   if(disposed)return;disposed=true;
   // Kill first to interrupt outstanding asynchronous pipe I/O; no blocking on UI thread.
   try{if(!proc.HasExited){proc.Kill();proc.WaitForExit(500);}}catch{}
   try{if(input!=null)input.Dispose();}catch{}try{if(output!=null)output.Dispose();}catch{}proc.Dispose();
  }
 }
 public static class LiveProbe {
  public static long Test(string cli,string model,string work,string a,string b){
   using(var live=new LiveSession(cli,model,work,CancellationToken.None)){
    var sw=Stopwatch.StartNew();
    using(var first=Render.Load(live.Screenshot(a,512,320,CancellationToken.None)))
    using(var second=Render.Load(live.Screenshot(b,512,320,CancellationToken.None))){
     if(first.Width!=512||second.Width!=512||first.Height!=320||second.Height!=320)throw new IOException("MCP screenshot size mismatch");
     int delta=0,contentA=0,contentB=0;int bgA=first.GetPixel(0,0).ToArgb(),bgB=second.GetPixel(0,0).ToArgb();
     for(int y=0;y<320;y+=8)for(int x=0;x<512;x+=8){int pa=first.GetPixel(x,y).ToArgb(),pb=second.GetPixel(x,y).ToArgb();if(pa!=pb)delta++;if(pa!=bgA)contentA++;if(pb!=bgB)contentB++;}
     if(delta<=20||contentA<=20||contentB<=20)throw new IOException("Live camera test did not show two distinct visible models");
    }return sw.ElapsedMilliseconds;
   }
  }
 }
 static class Render {
  const long MaxInput=128L*1024*1024, MaxCache=512L*1024*1024;
  internal static string Quote(string s) { return "\""+s+"\""; } // generated paths contain no quotes or trailing slash
  static string Hex(byte[] b) { return BitConverter.ToString(b).Replace("-","").ToLowerInvariant(); }
  internal static bool IsPng(string p,int width=0,int height=0) { try { using(var f=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) { byte[] b=new byte[24]; if(f.Length>32*1024*1024 || f.Read(b,0,24)!=24) return false; int w=(b[16]<<24)|(b[17]<<16)|(b[18]<<8)|b[19]; int h=(b[20]<<24)|(b[21]<<16)|(b[22]<<8)|b[23]; return b.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) && w>0 && h>0 && w<=4096 && h<=4096 && (width==0||w==width) && (height==0||h==height); } } catch {return false;} }
  internal static Bitmap Load(string p) { if(!IsPng(p)) throw new IOException("Invalid render PNG"); using(var f=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) using(var im=Image.FromStream(f,true,true)) return new Bitmap(im); }
  static int Run(string cli,string input,string output,string work, Stopwatch clock, CancellationToken ct, int budget, string camera=null,int width=0,int height=0) {
   var psi=new ProcessStartInfo(cli,"snapshot "+Quote(input)+" --out "+Quote(output)+" --no-grid --no-axes"+(width>0&&height>0?" --width "+width+" --height "+height:"")+(camera==null?"":" --camera "+Quote(camera.Replace("\"","\\\"")))) { UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=work,RedirectStandardOutput=true,RedirectStandardError=true };
   psi.EnvironmentVariables["TEMP"]=work; psi.EnvironmentVariables["TMP"]=work; psi.EnvironmentVariables["SOLIDCRAFT_GPU"]="auto";
   using(var p=new Process {StartInfo=psi}) { p.OutputDataReceived+=(s,e)=>{}; p.ErrorDataReceived+=(s,e)=>{if(camera!=null&&e.Data!=null&&e.Data.IndexOf("error",StringComparison.OrdinalIgnoreCase)>=0)Native.Log("Orbit CLI: "+e.Data.Substring(0,Math.Min(300,e.Data.Length)));}; p.Start(); Native.Log("CLI started"); try { p.BeginOutputReadLine(); p.BeginErrorReadLine(); while(!p.WaitForExit(100)) {ct.ThrowIfCancellationRequested(); if(clock.ElapsedMilliseconds>budget) throw new TimeoutException("Render timeout");} Native.Log("CLI exit="+p.ExitCode); return p.ExitCode; } finally { try {if(!p.HasExited) {p.Kill(); p.WaitForExit(500);}} catch {} } }
  }
  static void Trim(string dir,string keep) { try {var list=new DirectoryInfo(dir).GetFiles("*.png").OrderByDescending(x=>x.LastAccessTimeUtc).ToArray(); long total=list.Sum(x=>x.Length); foreach(var f in list.Reverse()) { if(total<=MaxCache) break; if(f.FullName==keep) continue; try {long n=f.Length; f.Delete(); total-=n;} catch {} } } catch {} }
  internal sealed class Prepared : IDisposable {
   internal string Cli, Root, Work, Model, Png, Ext;
   internal CameraFrame Frame;
   internal LiveSession Live; internal bool LiveDisabled;
   internal Stopwatch Clock=Stopwatch.StartNew();
   public void Dispose(){if(Live!=null){try{Live.Dispose();}catch{}Live=null;}try{if(Work!=null)Directory.Delete(Work,true);}catch{}}
  }
  static void Check(Prepared p,CancellationToken ct,int budget){ct.ThrowIfCancellationRequested();if(p.Clock.ElapsedMilliseconds>budget)throw new TimeoutException("Render budget exceeded");}
  internal static Prepared Prepare(IStream stream,string ext,CancellationToken ct,int budget){
   var p=new Prepared{Ext=ext};
   try {
    using(var k=Registry.CurrentUser.OpenSubKey(@"Software\SolidPreview")){p.Cli=k==null?null:k.GetValue("Cli") as string;}
    if(String.IsNullOrEmpty(p.Cli)||!File.Exists(p.Cli))throw new IOException("SolidCraft CLI not configured");
    p.Root=Native.Cache;Directory.CreateDirectory(p.Root);p.Work=Path.Combine(p.Root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(p.Work);p.Model=Path.Combine(p.Work,"model"+ext);
    string key;
    using(var sha=SHA256.Create())using(var f=new FileStream(p.Model,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
     // Keep v1 cache keys compatible with already rendered PNGs.
     byte[] meta=Encoding.UTF8.GetBytes("v1|iso|no-grid|no-axes|"+ext+"|"+p.Cli+"|"+File.GetLastWriteTimeUtc(p.Cli).Ticks+"|"+new FileInfo(p.Cli).Length+"|");
     sha.TransformBlock(meta,0,meta.Length,meta,0);byte[] buf=new byte[65536];IntPtr read=Marshal.AllocCoTaskMem(4);long total=0;
     try{stream.Seek(0,0,IntPtr.Zero);while(true){Check(p,ct,budget);Marshal.WriteInt32(read,0);stream.Read(buf,buf.Length,read);int n=Marshal.ReadInt32(read);if(n==0)break;if(n<0||n>buf.Length||(total+=n)>MaxInput)throw new IOException("Input exceeds 128 MiB");f.Write(buf,0,n);sha.TransformBlock(buf,0,n,buf,0);}}finally{Marshal.FreeCoTaskMem(read);}
     sha.TransformFinalBlock(new byte[0],0,0);key=Hex(sha.Hash);
    }
    p.Png=Path.Combine(p.Root,key+".png");Native.Log("Input copied "+ext);return p;
   }catch{p.Dispose();throw;}
  }
  static FileStream TryLock(string path){try{return new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){return null;}}
  static string InputFor(Prepared p){
   string ext=p.Ext;
   if(ext!=".stl"&&ext!=".obj"&&ext!=".3mf"&&ext!=".igs"&&ext!=".iges")return p.Model;
   string input=Path.Combine(p.Work,"import.json"),cmd=(ext==".igs"||ext==".iges")?"doc.open":"file.insert_mesh";
   string escaped=p.Model.Replace("\\","\\\\").Replace("\"","\\\"");
   File.WriteAllText(input,"{\"commands\":[{\"command\":\""+cmd+"\",\"params\":{\"path\":\""+escaped+"\"}}]}",new UTF8Encoding(false));return input;
  }
  static CameraFrame Measure(Prepared p,CancellationToken ct,int budget){
   FileStream slot=null;while(slot==null){Check(p,ct,budget);for(int i=0;i<2&&slot==null;i++)slot=TryLock(Path.Combine(p.Root,"slot-"+i+".lock"));if(slot==null)Thread.Sleep(50);}
   using(slot){
    Check(p,ct,budget);string input=InputFor(p);
    var psi=new ProcessStartInfo(p.Cli,"eval "+Quote(input)){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=p.Work,RedirectStandardOutput=true,RedirectStandardError=true};
    psi.EnvironmentVariables["TEMP"]=p.Work;psi.EnvironmentVariables["TMP"]=p.Work;psi.EnvironmentVariables["SOLIDCRAFT_GPU"]="auto";
    using(var proc=new Process{StartInfo=psi}){
     proc.Start();Native.Log("Orbit eval started");
     try{
      var output=proc.StandardOutput.ReadToEndAsync();var error=proc.StandardError.ReadToEndAsync();
      while(!proc.WaitForExit(100))Check(p,ct,budget);
      while(!output.IsCompleted||!error.IsCompleted){Check(p,ct,budget);Thread.Sleep(20);}
      Check(p,ct,budget);if(proc.ExitCode!=0)throw new IOException("SolidCraft eval failed: "+error.Result.Substring(0,Math.Min(300,error.Result.Length)));
      var frame=CameraFrame.Parse(output.Result);Native.Log("Orbit bounds ready");return frame;
     }finally{try{if(!proc.HasExited){proc.Kill();proc.WaitForExit(500);}}catch{}}
    }
   }
  }
  internal static void Warmup(Prepared p,CancellationToken ct){
   if(p.LiveDisabled||p.Live!=null)return;
   try{
    p.Live=new LiveSession(p.Cli,InputFor(p),p.Work,ct);
    try{p.Frame=p.Live.Measure(ct);}catch(OperationCanceledException){throw;}catch(TimeoutException){throw;}catch(Exception ex){Native.Log("Live measure fallback: "+ex.Message);p.Clock.Restart();p.Frame=Measure(p,ct,18000);}
    Native.Log("Live orbit preloaded");
   }catch(OperationCanceledException){throw;}catch(Exception ex){DisableLive(p,ex);}
  }
  static void DisableLive(Prepared p,Exception ex){
   p.LiveDisabled=true;Native.Log("Live orbit fallback: "+ex.GetType().Name+": "+ex.Message);if(p.Live!=null){p.Live.Dispose();p.Live=null;}
  }
  internal static Size PreviewSize(int w,int h,bool moving){
   double scale=Math.Min(1.0,(moving?960.0:2048.0)/Math.Max(w,h));
   return new Size(Math.Max(1,(int)Math.Round(w*scale)),Math.Max(1,(int)Math.Round(h*scale)));
  }
  internal static string SizedHome(Prepared p,CancellationToken ct,int w,int h){
   p.Clock.Restart();string key;using(var sha=SHA256.Create())key=Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(p.Png)+"|pane-iso-v1|"+w+"x"+h)));
   return Finish(p,ct,18000,Path.Combine(p.Root,key+".png"),null,w,h);
  }
  internal static string Orbit(Prepared p,CancellationToken ct,double yaw,double pitch,bool moving,int width,int height,double zoom,double panX,double panY,double panZ){
   p.Clock.Restart();
   if(p.Frame==null)p.Frame=Measure(p,ct,18000);
   string camera=p.Frame.ToJson(yaw*Math.PI/180.0,pitch*Math.PI/180.0,(double)width/height,zoom,panX,panY,panZ);
   string key=Path.GetFileNameWithoutExtension(p.Png);
   if(p.Live!=null&&!p.LiveDisabled){
    try{
     if(moving)return p.Live.Screenshot(camera,width,height,ct);
     string finalKey;using(var sha=SHA256.Create())finalKey=Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(key+"|live-final-v2|"+width+"x"+height+"|"+camera)));
     string final=Path.Combine(p.Root,finalKey+".png");
     if(IsPng(final)){try{File.SetLastAccessTimeUtc(final,DateTime.UtcNow);}catch{}return final;}
     string live=p.Live.Screenshot(camera,width,height,ct),temp=Path.Combine(p.Work,Guid.NewGuid().ToString("N")+".png");
     try{File.Copy(live,temp);try{File.Move(temp,final);}catch(IOException){if(!IsPng(final))return live;}Trim(p.Root,final);return final;}
     catch(IOException){return live;}finally{try{File.Delete(temp);}catch{}}
    }catch(OperationCanceledException){throw;}catch(Exception ex){DisableLive(p,ex);}
   }
   p.Clock.Restart();
   using(var sha=SHA256.Create())key=Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(key+"|orbit-v3|"+width+"x"+height+"|"+camera)));
   return Finish(p,ct,18000,Path.Combine(p.Root,key+".png"),camera,width,height);
  }
  internal static string Finish(Prepared p,CancellationToken ct,int budget,string png=null,string camera=null,int width=0,int height=0){
   png=png??p.Png;
   Check(p,ct,budget);
   if(IsPng(png)){try{File.SetLastAccessTimeUtc(png,DateTime.UtcNow);}catch{}Native.Log("Cache hit");return png;}
   // A fixed number of short-lived lock files, with per-bucket deduplication.
   int bucket=Convert.ToInt32(Path.GetFileName(png).Substring(0,2),16)%32;
   FileStream keyLock=null;while(keyLock==null){Check(p,ct,budget);if(IsPng(png))return png;keyLock=TryLock(Path.Combine(p.Root,"key-"+bucket+".lock"));if(keyLock==null)Thread.Sleep(50);}
   using(keyLock){
    if(IsPng(png))return png;
    FileStream slot=null;while(slot==null){Check(p,ct,budget);for(int i=0;i<2&&slot==null;i++)slot=TryLock(Path.Combine(p.Root,"slot-"+i+".lock"));if(slot==null)Thread.Sleep(50);}
    using(slot){
     if(IsPng(png))return png;
     string input=InputFor(p),temp=Path.Combine(p.Work,Guid.NewGuid().ToString("N")+".png"),ext=p.Ext;
     try { Check(p,ct,budget);if(Run(p.Cli,input,temp,p.Work,p.Clock,ct,budget,camera,width,height)!=0||!IsPng(temp,width,height))throw new IOException("SolidCraft snapshot failed ("+ext+")");
     Check(p,ct,budget);
     // A concurrent writer, including an older handler, may have published the same PNG.
     if(IsPng(png)){Native.Log("Cache published by another writer");return png;}
     try {File.Move(temp,png);}
     catch(IOException ex){
      if(IsPng(png)){Native.Log("Cache publish race resolved");return png;}
      // Replace invalid existing data atomically on the same volume, never delete a valid winner.
      int code=Marshal.GetHRForException(ex)&0xffff;
      if((code!=80&&code!=183)||!File.Exists(png))throw;
      if(!Native.MoveFileEx(temp,png,1)){
       int error=Marshal.GetLastWin32Error();if(IsPng(png)){Native.Log("Cache publish race resolved after replace");return png;}
       throw new IOException("Cache replace failed: Win32 "+error,ex);
      }
     }
     Trim(p.Root,png);Native.Log("Cache saved");return png;
     } finally {try{File.Delete(temp);}catch{}}
    }
   }
  }
  internal static Bitmap Fit(Bitmap src,int w,int h){double ratio=Math.Min((double)w/src.Width,(double)h/src.Height);var b=new Bitmap(Math.Max(1,(int)(src.Width*ratio)),Math.Max(1,(int)(src.Height*ratio)),PixelFormat.Format24bppRgb);try{using(var g=Graphics.FromImage(b)){g.Clear(Color.White);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(src,0,0,b.Width,b.Height);}return b;}catch{b.Dispose();throw;}}

 }
 [ComVisible(false)] public abstract class Base {
  protected readonly string ext; protected IStream stream; protected Base(string e){ext=e;}
  public int Initialize(IStream s,uint mode){ Native.Log(GetType().Name+" Initialize "+ext); if(s==null)return unchecked((int)0x80070057); if(stream!=null)return unchecked((int)0x800704DF); stream=s; return 0; }
 }
 [ComVisible(false)] public abstract class Thumb : Base,IInitializeWithStream,IThumbnailProvider {
  protected Thumb(string e):base(e){}
  public int GetThumbnail(uint cx,out IntPtr bitmap,out uint alpha){bitmap=IntPtr.Zero;alpha=0;Native.Log("Thumbnail enter "+ext);try{if(stream==null||cx==0)return unchecked((int)0x80070057);using(var p=Render.Prepare(stream,ext,CancellationToken.None,8000))using(var src=Render.Load(Render.Finish(p,CancellationToken.None,8000)))using(var fit=Render.Fit(src,(int)Math.Min(cx,4096u),(int)Math.Min(cx,4096u)))using(var b=new Bitmap(fit.Width,fit.Height,PixelFormat.Format32bppRgb)){using(var g=Graphics.FromImage(b)){g.DrawImageUnscaled(fit,0,0);}Native.Log("Thumbnail creating DIB "+b.Width+"x"+b.Height);bitmap=Native.MakeDib(b);alpha=1;Native.Log("Thumbnail DIB ready");return 0;}}catch(Exception ex){Native.Log("Thumbnail error "+ex.GetType().Name+" HRESULT=0x"+Marshal.GetHRForException(ex).ToString("X8")+": "+ex.Message);return unchecked((int)0x80004005);}finally{stream=null;Native.Log("Thumbnail leave");}}

 }
 [ComVisible(false)] public abstract class Preview : Base,IInitializeWithStream,IPreviewHandler,IOleWindow,IObjectWithSite {
  protected Preview(string e):base(e){Native.Log("Preview constructed v1.0.18 "+e);}
  IntPtr parent; RECT bounds; object site; PreviewSession session;
  public int SetWindow(IntPtr h,ref RECT r){parent=h;bounds=r;var s=session;if(s!=null)s.Update(h,r);Native.Log("Preview SetWindow "+ext);return 0;}
  public int SetRect(ref RECT r){bounds=r;var s=session;if(s!=null)s.Update(parent,r);Native.Log("Preview SetRect "+ext);return 0;}
  public int DoPreview(){
   Native.Log("Preview enter v1.0.18 "+ext);
   if(parent==IntPtr.Zero||stream==null||session!=null)return unchecked((int)0x80004005);
   Render.Prepared p=null;
   try{
    // Read the COM stream only on its owning apartment. The UI/render threads receive a private file.
    p=Render.Prepare(stream,ext,CancellationToken.None,18000);stream=null;
    var s=new PreviewSession(ext,parent,bounds,p);s.Start();session=s;p=null;
    Native.Log("Preview leave (UI scheduled)");return 0;
   }catch(Exception ex){if(p!=null)p.Dispose();Native.Log("Preview start error "+ex.GetType().Name+": "+ex.Message);return unchecked((int)0x80004005);}
  }
  public int Unload(){Native.Log("Preview unload enter");var s=session;session=null;stream=null;if(s!=null)s.Close();Native.Log("Preview unload leave");return 0;}
  public int SetFocus(){var s=session;if(s!=null)s.RequestFocus();return 0;}
  public int QueryFocus(out IntPtr h){var s=session;h=s==null?IntPtr.Zero:s.Focused;return 0;}
  public int TranslateAccelerator(ref MSG msg){return 1;}
  public int GetWindow(out IntPtr h){var s=session;h=s==null?IntPtr.Zero:s.Window;return h==IntPtr.Zero?unchecked((int)0x80004005):0;}
  public int ContextSensitiveHelp(bool e){return unchecked((int)0x80004001);}
  public int SetSite(object s){Native.Log("Preview SetSite "+ext);site=s;return 0;}
  public int GetSite(ref Guid iid,out IntPtr p){p=IntPtr.Zero;var s=site;if(s==null)return unchecked((int)0x80004005);IntPtr u=Marshal.GetIUnknownForObject(s);try{return Marshal.QueryInterface(u,ref iid,out p);}finally{Marshal.Release(u);}}
 }
 // The COM apartment is not assumed to pump ordinary window/timer messages.
 // The session owns its HWND, window messages, bitmaps, and render scheduling on a dedicated STA.
 // No COM call waits for the UI thread, and the UI thread never touches the COM stream or site.
 sealed class PreviewSession {
  const int WS_CHILD=0x40000000,WS_VISIBLE=0x10000000,SS_BITMAP=0x0E,SS_CENTERIMAGE=0x200,SS_NOTIFY=0x100;
  const int WS_EX_NOPARENTNOTIFY=0x00000004;
  const uint STM_SETIMAGE=0x0172;
  readonly string ext; readonly object gate=new object(); readonly CancellationTokenSource cancel=new CancellationTokenSource();
  readonly CancellationToken token; readonly Thread ui;
  IntPtr desiredParent, parent, hwnd, hbitmap, publishedWindow, publishedFocus;
  RECT desiredBounds,bounds; int imageW,imageH; bool focusRequested;
  volatile bool closed;
  Render.Prepared prepared; Task<string> task; Bitmap image;
  bool dragging,dragMoved,pending,home=true,orbitEnabled,subclassInstalled,firstFrame=true,warming;
  int lastX,lastY; double yaw=-45,pitch=35.264389682754654; long lastRenderStart;
  int dragButton; bool panning; long wheelDue;
  double zoom=1.0,panX,panY,panZ;
  void ResetFraming(){zoom=1.0;panX=panY=panZ=0;wheelDue=0;}
  void MarkInteraction(){home=false;pending=true;buttonRequest=false;viewRevision++;}
  void EndDrag(){if(dragging&&dragMoved)pending=true;dragging=false;dragMoved=false;panning=false;dragButton=0;}

  // A single orthographic orientation cube. Every visible face is divided into
  // 9 regions: one face, four edge strips and four corner patches. Regions on
  // neighboring faces share the same direction code, including hover feedback.
  const double CornerPitch=35.264389682754654;
  bool buttonRequest; int viewRevision, renderRevision;
  double cubeYaw=-45,cubePitch=CornerPitch,renderYaw=-45,renderPitch=CornerPitch;
  int hoverCode=-1,mouseX=-1,mouseY=-1,navOffsetX,navOffsetY;
  bool mouseTracking;
  Rectangle navPanel=Rectangle.Empty,homeRect=Rectangle.Empty;
  const int HomeHit=27, ArrowFirst=28, ArrowCount=4;
  // Screen-space order: top, right, bottom, left. The glyphs point inward.
  readonly Rectangle[] arrowRects=new Rectangle[ArrowCount];
  static bool FaceView(double y,double p){
   const double eps=0.00001;
   return Math.Abs(y-Math.Round(y/90.0)*90.0)<eps &&
     (Math.Abs(p)<eps||Math.Abs(Math.Abs(p)-90.0)<eps);
  }
  static int AdjacentFace(double y,double p,int side){
   double a=y*Math.PI/180,b=p*Math.PI/180;
   // Camera screen-right and screen-up axes from CubeProjection.Point.
   double rx=Math.Cos(a),ry=-Math.Sin(a);
   double ux=Math.Sin(a)*Math.Sin(b),uy=Math.Cos(a)*Math.Sin(b),uz=Math.Cos(b);
   double x=side==0?ux:side==1?rx:side==2?-ux:-rx;
   double v=side==0?uy:side==1?ry:side==2?-uy:-ry;
   double z=side==0?uz:side==1?0:side==2?-uz:0;
   return DirectionCode((int)Math.Round(x),(int)Math.Round(v),(int)Math.Round(z));
  }
  void SelectArrow(int side){
   if(side<0||side>=ArrowCount||!FaceView(cubeYaw,cubePitch))return;
   int code=AdjacentFace(cubeYaw,cubePitch,side);
   SelectCubeView(code);
   // At the poles retain the heading of the departing side, so the opposite
   // vertical arrow returns to it. Yaw is otherwise undefined at a pole.
   if(Math.Abs(pitch)==90.0)yaw=Math.Round(cubeYaw/90.0)*90.0;
   Native.Log("ViewCube adjacent side="+side+" direction="+code+" yaw="+yaw+" pitch="+pitch);
  }
  long resizeDue; bool adaptiveReady;
  sealed class CubeRegion { internal PointF[] Poly; internal int Code; }
  sealed class CubeFace {
   internal PointF[] Outline; internal PointF[] LabelQuad;
   internal string Label; internal double Facing; internal Color Fill;
  }
  readonly List<CubeRegion> cubeRegions=new List<CubeRegion>();
  static readonly string[,] faceLabels={{"RIGHT","LEFT"},{"FRONT","BACK"},{"BOTTOM","TOP"}};
  static readonly double[] splits={-1,-0.65,0.65,1};
  static int DirectionCode(int x,int y,int z){return (x+1)*9+(y+1)*3+z+1;}
  static void DirectionAngles(int code,out double y,out double p){
   int x=code/9-1,v=(code/3)%3-1,z=code%3-1;
   y=x==0&&v==0?0:Math.Atan2(-x,-v)*180/Math.PI;
   p=Math.Atan2(z,Math.Sqrt(x*x+v*v))*180/Math.PI;
  }
  void SelectHome(){
   EndDrag();if(Native.GetCapture()==hwnd)Native.ReleaseCapture();ResetFraming();
   yaw=-45;pitch=CornerPitch;home=true;pending=true;buttonRequest=true;viewRevision++;
   Native.Log("ViewCube home");
  }
  void SelectCubeView(int code){
   if(code<0||code>26||code==13)return;
   EndDrag();if(Native.GetCapture()==hwnd)Native.ReleaseCapture();ResetFraming();
   buttonRequest=true;viewRevision++;home=false;DirectionAngles(code,out yaw,out pitch);
   pending=true;Native.Log("ViewCube direction="+code+" yaw="+yaw.ToString("R",CultureInfo.InvariantCulture)+" pitch="+pitch.ToString("R",CultureInfo.InvariantCulture));
  }
  static bool InPolygon(PointF[] poly,float x,float y){
   // All regions are convex. A cross-product sign test works with either winding.
   bool pos=false,neg=false;
   for(int i=0;i<poly.Length;i++){
    PointF a=poly[i],b=poly[(i+1)%poly.Length];
    float cross=(b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X);
    if(cross>0.01f)pos=true;else if(cross< -0.01f)neg=true;
    if(pos&&neg)return false;
   }return true;
  }
  int CubeHit(int x,int y){
   if(navPanel.IsEmpty||!navPanel.Contains(x,y))return -1;
   if(homeRect.Contains(x,y))return HomeHit;
   for(int i=0;i<ArrowCount;i++)if(!arrowRects[i].IsEmpty&&arrowRects[i].Contains(x,y))return ArrowFirst+i;
   for(int i=cubeRegions.Count-1;i>=0;i--){var r=cubeRegions[i];if(InPolygon(r.Poly,x-navOffsetX,y-navOffsetY))return r.Code;}
   return -1;
  }
  bool NavInput(int x,int y,uint msg){
   bool inside=navPanel.Contains(x,y)&&!navPanel.IsEmpty;
   if(msg==0x0200){
    mouseX=x;mouseY=y;
    if(!mouseTracking){var t=new Native.TRACKMOUSEEVENT{Size=(uint)Marshal.SizeOf(typeof(Native.TRACKMOUSEEVENT)),Flags=2,Hwnd=hwnd};mouseTracking=Native.TrackMouseEvent(ref t);}
    int hit=CubeHit(x,y);if(hit!=hoverCode){hoverCode=hit;ShowImage();}
   }
   if(inside&&(msg==0x0201||msg==0x0203)){int hit=CubeHit(x,y);if(hit==HomeHit)SelectHome();else if(hit>=ArrowFirst&&hit<ArrowFirst+ArrowCount)SelectArrow(hit-ArrowFirst);else if(hit>=0)SelectCubeView(hit);}
   return inside;
  }
  sealed class CubeProjection {
   readonly double ca,sa,cb,sb; readonly float cx,cy,scale;
   internal CubeProjection(double yaw,double pitch,Rectangle r){
    double a=yaw*Math.PI/180,b=pitch*Math.PI/180;
    ca=Math.Cos(a);sa=Math.Sin(a);cb=Math.Cos(b);sb=Math.Sin(b);
    cx=r.X+r.Width/2f;cy=r.Y+r.Height/2f;
    // A constant scale across all orientations avoids distracting size changes.
    scale=(Math.Min(r.Width,r.Height)-14)/(2f*(float)Math.Sqrt(3));
   }
   internal double Facing(int axis,int sign){return sign*(axis==0?-sa*cb:axis==1?-ca*cb:sb);}
   internal PointF Point(int axis,int sign,int u,int us,int v,int vs,double s,double t){
    double[] q=new double[3];q[axis]=sign;q[u]=us*s;q[v]=vs*t;
    return new PointF(cx+(float)(q[0]*ca-q[1]*sa)*scale,cy+(float)(-q[0]*sa*sb-q[1]*ca*sb-q[2]*cb)*scale);
   }
   internal PointF[] Quad(int axis,int sign,int u,int us,int v,int vs,double l,double b,double r,double t){
    return new[]{Point(axis,sign,u,us,v,vs,l,t),Point(axis,sign,u,us,v,vs,r,t),Point(axis,sign,u,us,v,vs,r,b),Point(axis,sign,u,us,v,vs,l,b)};
   }
  }
  static void LabelFace(Graphics g,CubeFace face){
   // A face-aligned affine text transform. Skip heavily foreshortened faces.
   if(face.Facing<0.27)return;
   PointF[] q=face.LabelQuad;
   PointF[] dest=q[1].X<q[0].X?new[]{q[2],q[3],q[1]}:new[]{q[0],q[1],q[3]};
   var state=g.Save();
   try{
    using(var mat=new Matrix(new RectangleF(0,0,100,100),dest))
    using(var font=new Font("Segoe UI",22,FontStyle.Bold,GraphicsUnit.Pixel))
    using(var ink=new SolidBrush(Color.FromArgb(68,84,96)))
    using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap}){
     g.Transform=mat;g.DrawString(face.Label,font,ink,new RectangleF(0,0,100,100),format);
    }
   }finally{g.Restore(state);}
  }
  void DrawViewCube(Graphics g,Rectangle local,Rectangle projectionBounds){
   var projection=new CubeProjection(cubeYaw,cubePitch,projectionBounds);
   var faces=new List<CubeFace>();
   for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2){
    double facing=projection.Facing(axis,sign);if(facing<0.0001)continue;
    int u=axis==0?1:0,v=axis==2?1:2;
    int us=axis==0?sign:axis==1?-sign:1,vs=axis==2?sign:1;
    int shade=axis==2?(sign>0?231:181):axis==0?194:211;
    var face=new CubeFace{Outline=projection.Quad(axis,sign,u,us,v,vs,-1,-1,1,1),LabelQuad=projection.Quad(axis,sign,u,us,v,vs,-0.65,-0.65,0.65,0.65),Label=faceLabels[axis,sign>0?1:0],Facing=facing,Fill=Color.FromArgb(shade,Math.Min(255,shade+9),Math.Min(255,shade+16))};
    faces.Add(face);
    for(int j=0;j<3;j++)for(int i=0;i<3;i++){
     int[] n=new int[3];n[axis]=sign;n[u]=us*(i-1);n[v]=vs*(j-1);
     cubeRegions.Add(new CubeRegion{Code=DirectionCode(n[0],n[1],n[2]),Poly=projection.Quad(axis,sign,u,us,v,vs,splits[i],splits[j],splits[i+1],splits[j+1])});
    }
   }
   hoverCode=CubeHit(mouseX,mouseY);
   // The background is opaque so that labels remain legible over any CAD model.
   using(var bg=new SolidBrush(Color.FromArgb(248,250,252)))g.FillRectangle(bg,local);
   using(var line=new Pen(Color.FromArgb(83,100,114),1.2f))
   using(var hover=new SolidBrush(Color.FromArgb(255,216,102)))
   using(var accent=new Pen(Color.FromArgb(215,153,26),1.35f)){
    line.LineJoin=LineJoin.Round;accent.LineJoin=LineJoin.Round;
    foreach(var f in faces)using(var fill=new SolidBrush(f.Fill))g.FillPolygon(fill,f.Outline);
    foreach(var r in cubeRegions)if(r.Code==hoverCode)g.FillPolygon(hover,r.Poly);
    foreach(var f in faces)g.DrawPolygon(line,f.Outline);
    foreach(var r in cubeRegions)if(r.Code==hoverCode)g.DrawPolygon(accent,r.Poly);
    foreach(var f in faces)LabelFace(g,f);
   }
  }
  void DrawHome(Graphics g,Rectangle r){
   bool hot=homeRect.Contains(mouseX,mouseY);
   using(var fill=new SolidBrush(hot?Color.FromArgb(255,216,102):Color.FromArgb(248,250,252)))g.FillRectangle(fill,r);
   using(var pen=new Pen(hot?Color.FromArgb(215,153,26):Color.FromArgb(150,164,175),1f))g.DrawRectangle(pen,r.X+0.5f,r.Y+0.5f,r.Width-1,r.Height-1);
   float x=r.X,y=r.Y,d=r.Width;
   using(var ink=new Pen(Color.FromArgb(69,85,98),1.5f)){
    ink.LineJoin=LineJoin.Round;ink.StartCap=LineCap.Round;ink.EndCap=LineCap.Round;
    g.DrawLines(ink,new[]{new PointF(x+d*.20f,y+d*.47f),new PointF(x+d*.50f,y+d*.21f),new PointF(x+d*.80f,y+d*.47f)});
    g.DrawLines(ink,new[]{new PointF(x+d*.29f,y+d*.43f),new PointF(x+d*.29f,y+d*.78f),new PointF(x+d*.44f,y+d*.78f),new PointF(x+d*.44f,y+d*.58f),new PointF(x+d*.57f,y+d*.58f),new PointF(x+d*.57f,y+d*.78f),new PointF(x+d*.71f,y+d*.78f),new PointF(x+d*.71f,y+d*.43f)});
   }
  }
  void LayoutArrows(Rectangle local,Rectangle projectionBounds){
   if(!FaceView(cubeYaw,cubePitch))return;
   int cx=projectionBounds.Left+projectionBounds.Width/2,cy=projectionBounds.Top+projectionBounds.Height/2;
   // At a cardinal face the cube is a square of this half-width. Keep
   // buttons entirely in the four gaps between its edges and the panel rim.
   double half=(Math.Min(projectionBounds.Width,projectionBounds.Height)-14)/(2.0*Math.Sqrt(3.0));
   int faceHalf=(int)Math.Ceiling(half)+1;
   int gap=Math.Min(Math.Min(cx-local.Left,local.Right-cx),Math.Min(cy-local.Top,local.Bottom-cy))-faceHalf;
   int d=Math.Min(26,Math.Min(Math.Max(12,local.Width/6),gap-3));
   if(d<8)return;
   int dist=faceHalf+(gap-d)/2;
   var a=new[]{new Rectangle(cx-d/2,cy-dist-d,d,d),new Rectangle(cx+dist,cy-d/2,d,d),new Rectangle(cx-d/2,cy+dist,d,d),new Rectangle(cx-dist-d,cy-d/2,d,d)};
   for(int i=0;i<ArrowCount;i++){
    a[i].Offset(navOffsetX,navOffsetY);arrowRects[i]=a[i];
   }
  }
  void DrawArrows(Graphics g){
   for(int i=0;i<ArrowCount;i++){
    if(arrowRects[i].IsEmpty)continue;
    var r=arrowRects[i];r.Offset(-navOffsetX,-navOffsetY);
    bool hot=hoverCode==ArrowFirst+i;
    using(var fill=new SolidBrush(hot?Color.FromArgb(255,216,102):Color.FromArgb(235,240,245)))g.FillRectangle(fill,r);
    using(var outline=new Pen(hot?Color.FromArgb(215,153,26):Color.FromArgb(170,183,195),1f))g.DrawRectangle(outline,r.X+.5f,r.Y+.5f,r.Width-1,r.Height-1);
    float cx=r.X+r.Width/2f,cy=r.Y+r.Height/2f,d=r.Width;
    float dx=i==1?-1:i==3?1:0,dy=i==0?1:i==2?-1:0;
    using(var ink=new Pen(Color.FromArgb(69,85,98),Math.Max(1.25f,d*.085f))){
     ink.StartCap=LineCap.Round;ink.EndCap=LineCap.Round;ink.LineJoin=LineJoin.Round;
     var tip=new PointF(cx+dx*d*.25f,cy+dy*d*.25f);
     g.DrawLine(ink,cx-dx*d*.26f,cy-dy*d*.26f,tip.X,tip.Y);
     g.DrawLines(ink,new[]{new PointF(cx-dx*d*.03f-dy*d*.20f,cy-dy*d*.03f+dx*d*.20f),tip,new PointF(cx-dx*d*.03f+dy*d*.20f,cy-dy*d*.03f-dx*d*.20f)});
    }
   }
  }
  Bitmap ComposeNavigation(int w,int h){
   navPanel=Rectangle.Empty;homeRect=Rectangle.Empty;Array.Clear(arrowRects,0,ArrowCount);cubeRegions.Clear();
   if(!orbitEnabled||w<70||h<70)return Render.Fit(image,w,h);
   var canvas=new Bitmap(w,h,PixelFormat.Format24bppRgb);
   try{using(var g=Graphics.FromImage(canvas)){
    g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    using(var fit=Render.Fit(image,w,h))g.DrawImageUnscaled(fit,(w-fit.Width)/2,(h-fit.Height)/2);
    int size=Math.Min(156,Math.Min(w-8,Math.Min(h-8,Math.Max(86,Math.Min(w,h)/2))));
    int button=Math.Min(26,Math.Max(16,size/6));
    var local=new Rectangle(w-size-4,4,size,size);
    // Overlay Home inside the same navigation square, with hit-test priority.
    var hb=new Rectangle(local.Right-button-2,local.Top+2,button,button);
    // Keep the projected cube clear of Home even in a very small preview pane.
    // Every projected vertex is inside a circle of radius (size-14)/2.
    double clearance=Math.Sqrt(2.0)*(size/2.0-button-2)-2.0;
    int inset=Math.Max(0,(int)Math.Ceiling((size-14)/2.0-clearance));
    var projectionBounds=Rectangle.Inflate(local,-inset,-inset);
    navOffsetX=(W-w)/2;navOffsetY=(H-h)/2;
    navPanel=new Rectangle(local.X+navOffsetX,local.Y+navOffsetY,size,size);
    homeRect=new Rectangle(hb.X+navOffsetX,hb.Y+navOffsetY,hb.Width,hb.Height);
    LayoutArrows(local,projectionBounds);
    DrawViewCube(g,local,projectionBounds);
    DrawArrows(g);
    DrawHome(g,hb);
   }return canvas;}catch{canvas.Dispose();throw;}
  }
  static readonly Native.SubclassProc subclassProc=DispatchMouse;
  static readonly ConcurrentDictionary<IntPtr,PreviewSession> active=new ConcurrentDictionary<IntPtr,PreviewSession>();
  internal PreviewSession(string ext,IntPtr parent,RECT r,Render.Prepared p){
   this.ext=ext;desiredParent=parent;desiredBounds=r;prepared=p;token=cancel.Token;
   ui=new Thread(Run);ui.IsBackground=true;ui.Name="SolidPreview preview UI";ui.SetApartmentState(ApartmentState.STA);
  }
  internal void Start(){ui.Start();}
  internal void Close(){closed=true;}
  internal void Update(IntPtr p,RECT r){lock(gate){desiredParent=p;desiredBounds=r;}}
  internal void RequestFocus(){lock(gate){focusRequested=true;}}
  internal IntPtr Window{get{lock(gate){return publishedWindow;}}}
  internal IntPtr Focused{get{lock(gate){return publishedFocus;}}}
  int W{get{return Math.Max(1,bounds.Right-bounds.Left);}}
  int H{get{return Math.Max(1,bounds.Bottom-bounds.Top);}}
  static bool EqualRect(RECT a,RECT b){return a.Left==b.Left&&a.Top==b.Top&&a.Right==b.Right&&a.Bottom==b.Bottom;}
  static int Angle(double v){return (int)Math.Round(v);}
  static IntPtr DispatchMouse(IntPtr h,uint msg,IntPtr w,IntPtr l,UIntPtr id,UIntPtr data){
   try{PreviewSession s;if(active.TryGetValue(h,out s)&&s.orbitEnabled&&!s.closed){
    if(msg==0x0084)return new IntPtr(1); // HTCLIENT
    if(msg==0x0021)return new IntPtr(3); // MA_NOACTIVATE
    if(msg==0x007B)return IntPtr.Zero;
    if(s.Mouse(h,msg,w,l))return IntPtr.Zero;
   }}catch(Exception ex){Native.Log("Orbit input error "+ex.Message);}
   return Native.DefSubclassProc(h,msg,w,l);
  }
  bool Mouse(IntPtr h,uint msg,IntPtr w,IntPtr l){
   int x=unchecked((short)(l.ToInt64()&65535)),y=unchecked((short)((l.ToInt64()>>16)&65535));
   if(msg==0x02A3){mouseTracking=false;mouseX=mouseY=-1;if(hoverCode>=0){hoverCode=-1;ShowImage();}return false;}
   if(!dragging&&msg>=0x0200&&msg<=0x0209&&NavInput(x,y,msg))return true;
   switch(msg){
    case 0x020A: // WM_MOUSEWHEEL: wParam high word is signed wheel delta.
     int delta=unchecked((short)((w.ToInt64()>>16)&65535));
     double nextZoom=CameraNavigation.WheelZoom(zoom,delta);
     if(nextZoom!=zoom){zoom=nextZoom;wheelDue=Stopwatch.GetTimestamp()+Stopwatch.Frequency*3/20;MarkInteraction();}return true;
    case 0x0201:case 0x0204:case 0x0207:case 0x0209:
     if(dragging)return true;
     dragging=true;dragMoved=false;dragButton=msg==0x0209?0x0207:(int)msg;panning=dragButton==0x0207;
     lastX=x;lastY=y;Native.SetCapture(h);return true;
    case 0x0200:
     if(!dragging)return false;
     int dx=x-lastX,dy=y-lastY;lastX=x;lastY=y;
     if(dx!=0||dy!=0){
      if(panning){
       var d=CameraNavigation.PanDelta(yaw,pitch,zoom,W,H,dx,dy);
       panX=Math.Max(-100,Math.Min(100,panX+d[0]));panY=Math.Max(-100,Math.Min(100,panY+d[1]));panZ=Math.Max(-100,Math.Min(100,panZ+d[2]));
      }else{
       yaw=(yaw+dx*0.55)%360;pitch=Math.Max(-89.0,Math.Min(89.0,pitch+dy*0.55));
      }
      dragMoved=true;MarkInteraction();
     }return true;
    case 0x0202:case 0x0205:case 0x0208:
     if(dragging&&msg==dragButton+1){EndDrag();if(Native.GetCapture()==h)Native.ReleaseCapture();}return true;
    case 0x0215:case 0x001F:
     EndDrag();if(msg==0x001F&&Native.GetCapture()==h)Native.ReleaseCapture();return false;
    case 0x0203:case 0x0206:
     SelectHome();return true;
   }return false;
  }
  void ShowImage(){
   if(hwnd==IntPtr.Zero||image==null)return;
   using(var fit=ComposeNavigation(Math.Min(W,4096),Math.Min(H,4096))){
    IntPtr next=fit.GetHbitmap(Color.White);if(next==IntPtr.Zero)throw new IOException("GetHbitmap failed");
    Native.Log("Preview STM_SETIMAGE begin "+fit.Width+"x"+fit.Height);
    IntPtr old=Native.SendMessage(hwnd,STM_SETIMAGE,IntPtr.Zero,next);
    if(old!=IntPtr.Zero&&old!=hbitmap)Native.DeleteObject(old);
    if(hbitmap!=IntPtr.Zero)Native.DeleteObject(hbitmap);hbitmap=next;imageW=W;imageH=H;
    Native.InvalidateRect(hwnd,IntPtr.Zero,true);Native.Log("Preview STM_SETIMAGE end");
   }
  }
  void EnableOrbit(){
   orbitEnabled=false;
   try{
    using(var k=Registry.CurrentUser.OpenSubKey(@"Software\SolidPreview")){
     orbitEnabled=k!=null&&String.Equals(k.GetValue("Orbit") as string,"1")&&((k.GetValue("OrbitFormats") as string??"").Split(';').Contains(ext,StringComparer.OrdinalIgnoreCase));
     prepared.LiveDisabled=k==null||!((k.GetValue("LiveFormats") as string??"").Split(';').Contains(ext,StringComparer.OrdinalIgnoreCase));
    }
    if(orbitEnabled){active[hwnd]=this;subclassInstalled=Native.SetWindowSubclass(hwnd,subclassProc,new UIntPtr(1),UIntPtr.Zero);orbitEnabled=subclassInstalled;if(!subclassInstalled){PreviewSession ignore;active.TryRemove(hwnd,out ignore);}}
   }catch(Exception ex){orbitEnabled=false;Native.Log("Orbit initialization error "+ex.GetType().Name+": "+ex.Message);}
   Native.Log("Orbit enabled="+orbitEnabled);
  }
  void ShowInitialError(){
   try{var b=new Bitmap(450,140,PixelFormat.Format24bppRgb);using(var g=Graphics.FromImage(b))using(var font=new Font("Segoe UI",12)){g.Clear(Color.White);g.DrawString("SolidCraft preview failed.\nSee host-*.log for details.",font,Brushes.DimGray,new PointF(16,26));}if(image!=null)image.Dispose();image=b;ShowImage();}catch(Exception ex){Native.Log("Preview error image failed "+ex.Message);}
  }
  void StartWarmup(){
   if(prepared==null||prepared.LiveDisabled)return;
   var model=prepared;warming=true;
   task=Task.Factory.StartNew(()=>{Render.Warmup(model,token);return (string)null;},CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
  }
  void StartRender(bool reset,double y,double p,bool moving){
   renderRevision=viewRevision;renderYaw=reset?-45:y;renderPitch=reset?CornerPitch:p;var model=prepared;lastRenderStart=Stopwatch.GetTimestamp();
   bool initial=firstFrame,canOrbit=orbitEnabled;Size size=Render.PreviewSize(W,H,moving);
   double angleY=renderYaw,angleP=renderPitch,zoomAtRequest=zoom,px=panX,py=panY,pz=panZ;
   task=Task.Factory.StartNew(()=>{
    Native.Log("Preview render worker enter "+(reset?"home":y+","+p));
    try{
     if(initial){model.Clock.Restart();return Render.Finish(model,token,18000);}
     Native.Log("Preview requested size "+size.Width+"x"+size.Height);
     if(!canOrbit)return Render.SizedHome(model,token,size.Width,size.Height);
     try{return Render.Orbit(model,token,angleY,angleP,moving,size.Width,size.Height,zoomAtRequest,px,py,pz);}
     catch(OperationCanceledException){throw;}catch(Exception ex){if(!reset)throw;Native.Log("Adaptive home fallback: "+ex.Message);return Render.SizedHome(model,token,size.Width,size.Height);}
    }
    finally{Native.Log("Preview render worker leave");}
   },CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
  }
  void Tick(){
   IntPtr p;RECT r;bool focus;
   lock(gate){p=desiredParent;r=desiredBounds;focus=focusRequested;focusRequested=false;}
   if(p==IntPtr.Zero||!Native.IsWindow(p)||!Native.IsWindow(hwnd)){closed=true;return;}
   bool reparent=p!=parent;
   if(reparent){Native.SetParent(hwnd,p);parent=p;}
   if(reparent||!EqualRect(r,bounds)){
    bool sizeChanged=W!=Math.Max(1,r.Right-r.Left)||H!=Math.Max(1,r.Bottom-r.Top);
    bounds=r;Native.SetWindowPos(hwnd,IntPtr.Zero,bounds.Left,bounds.Top,W,H,0x0014);
    if(image!=null&&(imageW!=W||imageH!=H))ShowImage();
    if(sizeChanged)resizeDue=Stopwatch.GetTimestamp()+Stopwatch.Frequency/5;
   }
   if(focus)Native.SetFocus(hwnd);
   lock(gate){publishedFocus=Native.GetFocus();}
   if(task!=null){
    if(!task.IsCompleted)return;
    var done=task;task=null;
    if(warming){warming=false;try{done.GetAwaiter().GetResult();}catch(Exception ex){Native.Log("Live warmup error "+ex.Message);}}
    else{
    bool initial=firstFrame;firstFrame=false;
    try{
     string result=done.GetAwaiter().GetResult();
     if(!initial&&buttonRequest&&renderRevision!=viewRevision&&pending){Native.Log("Superseded view frame skipped");}
     else{
      Native.Log("Preview image loading");var next=Render.Load(result);
      var old=image;image=next;if(old!=null)old.Dispose();cubeYaw=renderYaw;cubePitch=renderPitch;ShowImage();Native.Log("Preview image displayed "+next.Width+"x"+next.Height);
      if(initial){adaptiveReady=true;EnableOrbit();resizeDue=Stopwatch.GetTimestamp()+Stopwatch.Frequency/5;if(orbitEnabled){ShowImage();StartWarmup();}}
     }
    }catch(Exception ex){Native.Log("Preview display error "+ex.GetType().Name+" HRESULT=0x"+Marshal.GetHRForException(ex).ToString("X8")+": "+ex.Message);if(initial){orbitEnabled=false;ShowInitialError();}}
    if(initial&&!adaptiveReady&&prepared!=null){prepared.Dispose();prepared=null;}
    }
    if(task!=null)return;
   }
   long now=Stopwatch.GetTimestamp();
   if(wheelDue!=0&&now>=wheelDue){wheelDue=0;pending=true;}
   bool moving=dragging||wheelDue!=0;
   if(adaptiveReady&&resizeDue!=0&&now>=resizeDue&&W>=32&&H>=32){resizeDue=0;pending=true;}
   if(!adaptiveReady||!pending||prepared==null||W<32||H<32)return;
   // Do not start resize-only renders while the splitter is still moving.
   if(resizeDue!=0&&now<resizeDue&&!moving&&!buttonRequest)return;
   if(moving&&(now-lastRenderStart)*1000.0/Stopwatch.Frequency<33)return;
   pending=false;resizeDue=0;double y=((yaw%360)+360)%360,pi=pitch;
   // Retain exact cube angles during zoom/pan; do not round away corner views.
   Native.Log("Orbit request "+(home?"home":y+","+pi)+" zoom="+zoom.ToString("R",CultureInfo.InvariantCulture)+" pan="+panX+","+panY+","+panZ);StartRender(home,y,pi,moving);
  }
  void Run(){
   Native.Log("Preview UI thread start v1.0.18");
   try{
    if(closed)return;
    // Initialize this thread's Windows message queue before creating its child.
    MSG msg;Native.PeekMessage(out msg,IntPtr.Zero,0,0,0);
    lock(gate){parent=desiredParent;bounds=desiredBounds;}
    if(parent==IntPtr.Zero||!Native.IsWindow(parent))throw new IOException("Preview parent is missing");
    Native.Log("Preview UI creating child "+W+"x"+H);
    hwnd=Native.CreateWindowEx(WS_EX_NOPARENTNOTIFY,"STATIC","",WS_CHILD|WS_VISIBLE|SS_BITMAP|SS_CENTERIMAGE|SS_NOTIFY,bounds.Left,bounds.Top,W,H,parent,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
    if(hwnd==IntPtr.Zero)throw new IOException("CreateWindowEx failed: "+Marshal.GetLastWin32Error());
    lock(gate){publishedWindow=hwnd;}Native.Log("Preview UI child created");
    if(closed)return;
    StartRender(true,0,0,false);Native.Log("Preview UI pump started");
    while(!closed){
     // Own the native message loop: do not rely on WM_TIMER in the COM apartment.
     for(int i=0;i<128&&Native.PeekMessage(out msg,IntPtr.Zero,0,0,1);i++){
      if(msg.message==0x0012){closed=true;break;}
      Native.TranslateMessage(ref msg);Native.DispatchMessage(ref msg);
     }
     if(closed)break;
     Tick();if(closed)break;
     // Wake for native input or within 16 ms to publish render completion.
     // No COM thread is blocked waiting for the preview window.
     if(Native.MsgWaitForMultipleObjectsEx(0,IntPtr.Zero,16,0x04FF,4)==0xffffffff)Thread.Sleep(16);
    }
   }catch(Exception ex){Native.Log("Preview UI error "+ex.GetType().Name+" HRESULT=0x"+Marshal.GetHRForException(ex).ToString("X8")+": "+ex.Message);}
   finally{
    closed=true;cancel.Cancel();lock(gate){publishedWindow=IntPtr.Zero;publishedFocus=IntPtr.Zero;}
    try{
     if(hwnd!=IntPtr.Zero){
      PreviewSession ignore;active.TryRemove(hwnd,out ignore);
      if(Native.IsWindow(hwnd)){
       if(subclassInstalled)Native.RemoveWindowSubclass(hwnd,subclassProc,new UIntPtr(1));
       if(Native.GetCapture()==hwnd)Native.ReleaseCapture();
       IntPtr old=Native.SendMessage(hwnd,STM_SETIMAGE,IntPtr.Zero,IntPtr.Zero);
       if(old!=IntPtr.Zero&&old!=hbitmap)Native.DeleteObject(old);
       Native.DestroyWindow(hwnd);
      }hwnd=IntPtr.Zero;
     }
    }catch(Exception ex){Native.Log("Preview UI destroy error "+ex.Message);}
    finally{
     if(hbitmap!=IntPtr.Zero)Native.DeleteObject(hbitmap);if(image!=null)image.Dispose();
     var model=prepared;prepared=null;var running=task;task=null;
     if(running!=null)running.ContinueWith(t=>{if(t.IsFaulted){var ignored=t.Exception;}if(model!=null)model.Dispose();cancel.Dispose();},TaskScheduler.Default);
     else{if(model!=null)model.Dispose();cancel.Dispose();}
     Native.Log("Preview UI thread exit");
    }
   }
  }
 }
}
namespace SolidPreview {
[ComVisible(true), Guid("064d6d4e-5233-4c7f-924c-9299a5f09513"), ClassInterface(ClassInterfaceType.None)] public sealed class FSTLPreview : Preview { public FSTLPreview():base(".stl"){} }
[ComVisible(true), Guid("1a19a1f4-7f5a-4d7a-bd90-f55dee3ebc30"), ClassInterface(ClassInterfaceType.None)] public sealed class FSTLThumb : Thumb { public FSTLThumb():base(".stl"){} }
[ComVisible(true), Guid("17ee02dd-fa74-43f3-b190-2de931ff443a"), ClassInterface(ClassInterfaceType.None)] public sealed class FSTPPreview : Preview { public FSTPPreview():base(".stp"){} }
[ComVisible(true), Guid("54229725-318d-4708-9d45-4d3421d6c5e1"), ClassInterface(ClassInterfaceType.None)] public sealed class FSTPThumb : Thumb { public FSTPThumb():base(".stp"){} }
[ComVisible(true), Guid("26d0dbd4-d300-462d-9cbf-e4c7cb48e211"), ClassInterface(ClassInterfaceType.None)] public sealed class FSTEPPreview : Preview { public FSTEPPreview():base(".step"){} }
[ComVisible(true), Guid("a8c3f70c-66e9-4eec-81cc-6d0de456b307"), ClassInterface(ClassInterfaceType.None)] public sealed class FSTEPThumb : Thumb { public FSTEPThumb():base(".step"){} }
[ComVisible(true), Guid("e7f52ec4-b300-4787-9cc9-59ce5e9c05dc"), ClassInterface(ClassInterfaceType.None)] public sealed class FOBJPreview : Preview { public FOBJPreview():base(".obj"){} }
[ComVisible(true), Guid("0876d9a4-3387-4856-96dc-7ad02ed67f9c"), ClassInterface(ClassInterfaceType.None)] public sealed class FOBJThumb : Thumb { public FOBJThumb():base(".obj"){} }
[ComVisible(true), Guid("a6b61bdf-7271-43c0-b236-5f31cad32af5"), ClassInterface(ClassInterfaceType.None)] public sealed class F3MFPreview : Preview { public F3MFPreview():base(".3mf"){} }
[ComVisible(true), Guid("2c5361fb-a224-4bed-b1f3-45c12fc815f0"), ClassInterface(ClassInterfaceType.None)] public sealed class F3MFThumb : Thumb { public F3MFThumb():base(".3mf"){} }
[ComVisible(true), Guid("da531fce-9297-4b21-961a-5b0477398d78"), ClassInterface(ClassInterfaceType.None)] public sealed class FIGSPreview : Preview { public FIGSPreview():base(".igs"){} }
[ComVisible(true), Guid("4d35a185-bcdf-4711-8dc9-a66444af05ca"), ClassInterface(ClassInterfaceType.None)] public sealed class FIGSThumb : Thumb { public FIGSThumb():base(".igs"){} }
[ComVisible(true), Guid("83be2b62-aa17-4012-b613-5bfeaa579ee3"), ClassInterface(ClassInterfaceType.None)] public sealed class FIGESPreview : Preview { public FIGESPreview():base(".iges"){} }
[ComVisible(true), Guid("0ed1b648-314a-4eef-a3bb-3017556bbf5c"), ClassInterface(ClassInterfaceType.None)] public sealed class FIGESThumb : Thumb { public FIGESThumb():base(".iges"){} }
[ComVisible(true), Guid("aefda98d-4234-4f96-8ac8-1d2f51786f7b"), ClassInterface(ClassInterfaceType.None)] public sealed class FSCPreview : Preview { public FSCPreview():base(".sc"){} }
[ComVisible(true), Guid("de7b9193-b47c-4b99-b378-2260375eef50"), ClassInterface(ClassInterfaceType.None)] public sealed class FSCThumb : Thumb { public FSCThumb():base(".sc"){} }
[ComVisible(true), Guid("204da88b-d7f3-4f04-b26c-0df4ee54888b"), ClassInterface(ClassInterfaceType.None)] public sealed class FSOLIDCRAFTPreview : Preview { public FSOLIDCRAFTPreview():base(".solidcraft"){} }
[ComVisible(true), Guid("0fdd0788-c656-4250-b609-908f41b71de4"), ClassInterface(ClassInterfaceType.None)] public sealed class FSOLIDCRAFTThumb : Thumb { public FSOLIDCRAFTThumb():base(".solidcraft"){} }
}
