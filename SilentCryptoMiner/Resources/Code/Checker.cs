﻿using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Resources;
using System.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Management;
#if DefDebug
using System.Windows.Forms;
#endif

public partial class _rChecker_
{
    public static void Main()
    {
        try
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine("Not run as Administrator, only non-administrator miners can be searched for.");
            }
            var _rconnection_ = new ConnectionOptions();
            _rconnection_.Impersonation = ImpersonationLevel.Impersonate;
            var _rscope_ = new ManagementScope(@"\root\cimv2", _rconnection_);
            _rscope_.Connect();

            var _rsearcher_ = new ManagementObjectSearcher(_rscope_, new ObjectQuery("Select CommandLine from Win32_Process")).Get();
            bool _rwdrunning_ = false;
            foreach (ManagementObject _rmemObj_ in _rsearcher_)
            {
                if (_rmemObj_ != null && _rmemObj_["CommandLine"] != null && _rmemObj_["CommandLine"].ToString().Contains(_rGetString_("#WATCHDOGID")))
                {
                    _rwdrunning_ = true;
                    break;
                }
            }
            Console.WriteLine("Watchdog Running: " + (_rwdrunning_ ? "Yes" : "No"));

            Console.WriteLine("Miners:");
            _rsearcher_ = new ManagementObjectSearcher(_rscope_, new ObjectQuery("Select CommandLine from Win32_Process")).Get();
            foreach (ManagementObject _rmemObj_ in _rsearcher_)
            {
                if (_rmemObj_ != null && _rmemObj_["CommandLine"] != null && _rmemObj_["CommandLine"].ToString().Contains(_rGetString_("#MINERID")))
                {
                    try
                    {
                        foreach(string _rminer_ in _rmemObj_["CommandLine"].ToString().Split(' '))
                        {
                            string _rdecrypted_ = _rUnamlibDecrypt_(_rminer_);
                            if (!string.IsNullOrEmpty(_rdecrypted_))
                            {
                                Console.WriteLine(_rdecrypted_);
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }

            string _rgpu_ = "";
            Console.WriteLine("GPUs:");
            _rsearcher_ = new ManagementObjectSearcher(_rscope_, new ObjectQuery("SELECT Name, VideoProcessor FROM Win32_VideoController")).Get();
            foreach (ManagementObject _rmemObj_ in _rsearcher_)
            {
                _rgpu_ += " " + _rmemObj_["Name"];
                Console.WriteLine(" " + _rmemObj_["Name"]);
            }

            Console.WriteLine("Compatible GPU found: " + (_rgpu_.IndexOf("nvidia", StringComparison.OrdinalIgnoreCase) >= 0 || _rgpu_.IndexOf("amd", StringComparison.OrdinalIgnoreCase) >= 0));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
        Console.ReadKey();
    }

    public static string _rGetString_(string _rarg1_)
    {
        return Encoding.UTF8.GetString(_rAESMethod_(Convert.FromBase64String(_rarg1_)));
    }

    public static byte[] _rAESMethod_(byte[] _rarg1_, bool _rarg2_ = false)
    {
        var _rarg3_ = Encoding.ASCII.GetBytes("#IV");
        var _rarg4_ = new Rfc2898DeriveBytes("#KEY", Encoding.ASCII.GetBytes("#SALT"), 100);
        var _rarg5_ = new RijndaelManaged() { KeySize = 256, Mode = CipherMode.CBC };
        var _rarg6_ = _rarg2_ ? _rarg5_.CreateEncryptor(_rarg4_.GetBytes(16), _rarg3_) : _rarg5_.CreateDecryptor(_rarg4_.GetBytes(16), _rarg3_);
        using (var _rarg7_ = new MemoryStream())
        {
            using (var _rarg8_ = new CryptoStream(_rarg7_, _rarg6_, CryptoStreamMode.Write))
            {
                _rarg8_.Write(_rarg1_, 0, _rarg1_.Length);
                _rarg8_.Close();
            }

            return _rarg7_.ToArray();
        }
    }

    public static string _rUnamlibDecrypt_(string _rplainText_)
    {
        try
        {
            var _rinput_ = Convert.FromBase64String(_rplainText_);
            using (var _rmStream_ = new MemoryStream())
            {
                using (var _rcStream_ = new CryptoStream(_rmStream_, new RijndaelManaged() { KeySize = 256, BlockSize = 128, Mode = CipherMode.CBC, Padding = PaddingMode.Zeros }.CreateDecryptor(Encoding.ASCII.GetBytes(_rGetString_("#UNAMKEY")), Encoding.ASCII.GetBytes(_rGetString_("#UNAMIV"))), CryptoStreamMode.Write))
                {
                    _rcStream_.Write(_rinput_, 0, _rinput_.Length);
                    _rcStream_.FlushFinalBlock();
                    _rcStream_.Close();
                }
                return Encoding.UTF8.GetString(_rmStream_.ToArray());
            }
        }catch {}
        return "";
    }
}