﻿using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SharpPhysFS
{
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate int InitDelegate();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void DeinitDelegate();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr MallocDelegate(ulong size);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate IntPtr ReallocDelegate(IntPtr ptr, ulong size);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void FreeDelegate(IntPtr ptr);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void StringCallback(IntPtr data, string str);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  public delegate void EnumFilesCallback(IntPtr data, string origdir, string fname);

  [StructLayout(LayoutKind.Sequential)]
  public struct ArchiveInfo
  {
    [MarshalAs(UnmanagedType.LPStr)]
    public string extension;

    [MarshalAs(UnmanagedType.LPStr)]
    public string description;

    [MarshalAs(UnmanagedType.LPStr)]
    public string author;

    [MarshalAs(UnmanagedType.LPStr)]
    public string url;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct Version
  {
    public byte major;
    public byte minor;
    public byte patch;
  }

  [StructLayout(LayoutKind.Sequential)]
  public class Allocator
  {
    [MarshalAs(UnmanagedType.FunctionPtr)]
    public InitDelegate Init;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public DeinitDelegate Deinit;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public MallocDelegate Malloc;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public ReallocDelegate Realloc;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public FreeDelegate Free;
  }

  static class DynamicLoader
  {
    #region Windows
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);
    #endregion

    #region Unix
    [DllImport("libdl.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlopen")]
    public static extern IntPtr unix_dlopen(string filename, int flags);

    [DllImport("libdl.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlsym")]
    public static extern IntPtr unix_dlsym(IntPtr handle, string symbol);

    [DllImport("libdl.so", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlclose")]
    public static extern bool unix_dlclose(IntPtr handle);
    #endregion

    #region OSX
    [DllImport("libdyld.dylib", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlopen")]
    public static extern IntPtr osx_dlopen(string filename, int flags);

    [DllImport("libdyld.dylib", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlsym")]
    public static extern IntPtr osx_dlsym(IntPtr handle, string symbol);

    [DllImport("libdyld.dylib", CallingConvention = CallingConvention.Cdecl, EntryPoint = "dlclose")]
    public static extern bool osx_dlclose(IntPtr handle);
    #endregion
  }

  class Interop
    : IDisposable
  {
    InvalidOperationException initException = new InvalidOperationException("Callbacks not initialized yet");
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FnGetLinkedVersion(ref Version v);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnInit(string argv0);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnDeinit();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnClose(IntPtr ptr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr FnSupportedArchiveTypes();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FnFreeList(IntPtr h);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr FnGetLastError();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FnPermitSymbolicLinks(int permit);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnSetWriteDir(string s);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnAddToSearchPath(string s, int i);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnSetSaneConfig(string s1, string s2, string s3, int i1, int i2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr FnEnumerateFiles(string s);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long FnGetLastModTime(string s);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long FnRead(IntPtr ptr1, IntPtr ptr2, uint i1, uint i2);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long FnTell(IntPtr ptr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnSeek(IntPtr ptr, ulong u);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate long FnFileLength(IntPtr ptr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnSetAllocator(Allocator alloc);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int FnMount(string s1, string s2, int i);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FnGetCdRomDirsCallback(StringCallback c, IntPtr ptr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FnEnumerateFilesCallback(string s, EnumFilesCallback c, IntPtr ptr);

    // When the callbacks are not yet initialized, instead of throwing a
    // null reference exception we explain the problem.
    // I think it makes for a more graceful failure.
    public FnGetLinkedVersion PHYSFS_getLinkedVersion             => throw initException;
    public FnInit PHYSFS_init                                     => throw initException; 
    public FnDeinit PHYSFS_deinit                                 => throw initException;
    public FnSupportedArchiveTypes PHYSFS_supportedArchiveTypes   => throw initException; 
    public FnFreeList PHYSFS_freeList                             => throw initException; 
    public FnGetLastError PHYSFS_getLastError                     => throw initException; 
    public FnGetLastError PHYSFS_getDirSeparator                  => throw initException; 
    public FnPermitSymbolicLinks PHYSFS_permitSymbolicLinks       => throw initException; 
    public FnSupportedArchiveTypes PHYSFS_getCdRomDirs            => throw initException; 
    public FnGetLastError PHYSFS_getBaseDir                       => throw initException; 
    public FnGetLastError PHYSFS_getUserDir                       => throw initException; 
    public FnGetLastError PHYSFS_getWriteDir                      => throw initException; 
    public FnSetWriteDir PHYSFS_setWriteDir                       => throw initException; 
    public FnAddToSearchPath PHYSFS_addToSearchPath               => throw initException; 
    public FnSetWriteDir PHYSFS_removeFromSearchPath              => throw initException; 
    public FnSupportedArchiveTypes PHYSFS_getSearchPath           => throw initException; 
    public FnSetSaneConfig PHYSFS_setSaneConfig                   => throw initException; 
    public FnSetWriteDir PHYSFS_mkdir                             => throw initException; 
    public FnSetWriteDir PHYSFS_delete                            => throw initException; 
    public FnEnumerateFiles PHYSFS_getRealDir                     => throw initException; 
    public FnEnumerateFiles PHYSFS_enumerateFiles                 => throw initException; 
    public FnSetWriteDir PHYSFS_exists                            => throw initException; 
    public FnSetWriteDir PHYSFS_isDirectory                       => throw initException; 
    public FnSetWriteDir PHYSFS_isSymbolicLink                    => throw initException; 
    public FnGetLastModTime PHYSFS_getLastModTime                 => throw initException; 
    public FnEnumerateFiles PHYSFS_openWrite                      => throw initException; 
    public FnEnumerateFiles PHYSFS_openAppend                     => throw initException; 
    public FnEnumerateFiles PHYSFS_openRead                       => throw initException; 
    public FnClose PHYSFS_close                                   => throw initException; 
    public FnRead PHYSFS_read                                     => throw initException; 
    public FnRead PHYSFS_write                                    => throw initException; 
    public FnClose PHYSFS_eof                                     => throw initException; 
    public FnTell PHYSFS_tell                                     => throw initException; 
    public FnSeek PHYSFS_seek                                     => throw initException; 
    public FnFileLength PHYSFS_fileLength                         => throw initException; 
    public FnSeek PHYSFS_setBuffer                                => throw initException; 
    public FnClose PHYSFS_flush                                   => throw initException; 
    public FnDeinit PHYSFS_isInit                                 => throw initException; 
    public FnDeinit PHYSFS_symbolicLinksPermitted                 => throw initException; 
    public FnSetAllocator PHYSFS_setAllocator                     => throw initException; 
    public FnMount PHYSFS_mount                                   => throw initException; 
    public FnEnumerateFiles PHYSFS_getMountPoint                  => throw initException; 
    public FnGetCdRomDirsCallback PHYSFS_getCdRomDirsCallback     => throw initException; 
    public FnGetCdRomDirsCallback PHYSFS_getSearchPathCallback    => throw initException; 
    public FnEnumerateFilesCallback PHYSFS_enumerateFilesCallback => throw initException; 

    public Interop()
      : this("physfs.dll", "libphysfs.dylib", "libphysfs.so")
    { }

    public Interop(string libname)
      : this($"{libname}.dll", $"{libname}.dylib", $"{libname}.so")
    { }

    Func<string, IntPtr> loadLibrary;
    Func<IntPtr, string, IntPtr> loadSymbol;
    Func<IntPtr, bool> freeLibrary;
    IntPtr library;

    public Interop(string winlib, string maclib, string unixlib)
    {
      /* This method is used to dynamically load the physfs
       * library. It works by detecting the current platform
       * and deciding whether to use LoadLibrary/GetProcAddr
       * on Windows or dlopen/dlsym on Linux and OSX.
       * The the class is then scanned using reflection
       * to populate all the callbacks with the right function
       * pointers from the loaded library
       */

      string libraryName;

      if (Environment.OSVersion.Platform == PlatformID.Win32NT)
      {
        loadLibrary = DynamicLoader.LoadLibrary;
        loadSymbol = DynamicLoader.GetProcAddress;
        freeLibrary = DynamicLoader.FreeLibrary;
        libraryName = winlib;
      }
      else if(Environment.OSVersion.Platform == PlatformID.MacOSX)
      {
        loadLibrary = n => DynamicLoader.osx_dlopen(n, 1);
        loadSymbol = DynamicLoader.osx_dlsym;
        freeLibrary = DynamicLoader.osx_dlclose;
        libraryName = maclib;
      }
      else
      {
        loadLibrary = n => DynamicLoader.unix_dlopen(n, 1);
        loadSymbol = DynamicLoader.unix_dlsym;
        freeLibrary = DynamicLoader.unix_dlclose;
        libraryName = unixlib;
      }

      library = loadLibrary(libraryName);
      if (library == IntPtr.Zero)
      {
        throw new PhysFSLibNotFound();
      }

      var fields = typeof(Interop).GetFields();

      foreach(var field in fields.Where(x => x.Name.StartsWith("PHYSFS_")))
      {
        var funcPtr = loadSymbol(library, field.Name);
        var del = Marshal.GetDelegateForFunctionPointer(funcPtr, field.FieldType);

        field.SetValue(this, del);
      }
    }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        /*if (disposing)
        {
          // TODO: dispose managed state (managed objects).
        }*/

        freeLibrary(library);

        disposedValue = true;
      }
    }

    public void Dispose()
    {
      Dispose(true);
    }
    #endregion
  }
}
