// ============================================================================
// Copyright (c) 2010  IOSS GmbH
// All Rights Reserved.
// ============================================================================


// ============================================================================
//
//      Wid110Lib  -  WID110 C# based library
//
// ============================================================================
//
//      File:     Wid110Lib.cs               Type:         Implementation
//
//      Date:     05.02.2010                 Last Change:  09.11.2011
//
//      Author:   Thomas M. Schlageter
//                Silvio Robel
//
//      Methods:  Wid110Lib             -  constructor
//                ~Wid110Lib            -  destructor
//
//                FCreateDll            -  create WID110Lib instance
//                FDestroyDll           -  destroy WID110Lib instance
//                FExit                 -  terminate connection and exit
//                FGetCodeQualityBCR    -  get code qualitiy for BCR codes
//                FGetCodeQualityDMR    -  get code qualitiy for DMR codes
//                FGetCodeQualityLast   -  get code qualitiy for LAST code
//                FGetCodeQualityOCR    -  get code qualitiy for OCR codes
//                FGetErrorDescription  -  get error description
//                FGetLastError         -  get the last error number
//                FGetVersionParam      -  return sensor/interface version
//                FGetVersion           -  return library version
//                FGetWaferId           -  get the last BCR/OCR decode result
//                FInit                 -  initialize library and connect
//                FIsInitialized        -  check for initialized state
//                FLiveGetImage         -  take single image with parameters
//                FLiveRead             -  perform a live read
//                FLoadRecipes          -  load parameters by sending a file
//                FLoadRecipesToSlot    -  load parameters by sending a file
//                FProcessGetImage      -  get image from process trigger
//                FProcessRead          -  perform a process read
//                FSwitchOverlay        -  switch overlay on/off
//				  FGetCodeTime		    -  get certain time parameter
//
//
//      Auxiliary methods:
//
//                getErrno              -  get error number
//                getLastExcp           -  get the last exception message
//                getReadOK             -  get result read state
//                getTmpImage           -  return temporary image name
//                isException           -  return exception state
//
// ============================================================================

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

using System.Drawing.Imaging;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.IOSS
{
    public class Wid110Lib : IDisposable
    {
        #region [ Private Fields ]
        // ====================================================================
        /// <summary>
        /// Private member variables.
        /// </summary>
        // ====================================================================

        private string version;
        private string versParam;

        private string tmpImage = Wid110LibConst.tmpImage;
        private string lastExcp;

        private int errno = Wid110LibConst.ecNone;
        private int readOK = Wid110LibConst.rcError;

        private IntPtr dll;
        #endregion

        #region [ DLL Method Prototypes ]
        // ====================================================================
        /// <summary>
        /// Imported method prototypes.
        /// </summary>
        // ====================================================================

        [DllImport("wid110Lib.dll")]
        private static extern IntPtr FuncCreateDll();

        [DllImport("wid110Lib.dll")]
        private static extern int FuncDestroyDll(IntPtr objptr);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncInit(IntPtr objptr,
                                           string cpIPAddress);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncIsInitialized(IntPtr objptr);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncCheckConnection(IntPtr objptr,
                                                    string cpIPAddress,
                                                    int timeout);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetVersionParam(IntPtr objptr,
                                                      StringBuilder cVersion,
                                                      int nMaxLen);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetVersion(IntPtr objptr,
                                                 StringBuilder cVersion,
                                                 int nMaxLen);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncSwitchOverlay(IntPtr objptr,
                                                    int bOnOff);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncExit(IntPtr objptr);

        // exchange parameter functions
        [DllImport("wid110Lib.dll")]
        private static extern int FuncSetImageCapture(IntPtr objptr, IntPtr pCapture);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetImageCapture(IntPtr objptr, IntPtr pCapture);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncSetParamOCR(IntPtr objptr, IntPtr pOcr, IntPtr capture);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetParamOCR(IntPtr objptr, IntPtr pOcr, IntPtr capture);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncSetParamBCR(IntPtr objptr, IntPtr pBcr, IntPtr capture);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetParamBCR(IntPtr objptr, IntPtr pBcr, IntPtr capture);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncSetParamDMR(IntPtr objptr, IntPtr pDmr, IntPtr capture);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetParamDMR(IntPtr objptr, IntPtr pDmr, IntPtr capture);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncLiveGetImage(IntPtr objptr,
                                                   string cpFileName,
                                                   int nChannel,
                                                   int nIntensity,
                                                   int nColor);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncLiveRead(IntPtr objptr);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncLiveGetImageRead(IntPtr objptr,
                                                        string cpFileName,
                                                        int nChannel,
                                                        int nIntensity,
                                                        int nColor);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncProcessRead(IntPtr objptr);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncProcessGetImage(IntPtr objptr,
                                                      string cpFileName,
                                                      int nTypeImage);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetWaferId(IntPtr objptr,
                                                 StringBuilder cReadId,
                                                 int nMaxLen,
                                                 IntPtr bReadSuccessful);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetCodeQualityOCR(IntPtr objptr,
                                                        IntPtr pnQuality);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetCodeQualityBCR(IntPtr objptr,
                                                        IntPtr pnQuality);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetCodeQualityDMR(IntPtr objptr,
                                                        IntPtr pnQuality);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetCodeQualityLast(IntPtr objptr,
                                                         IntPtr pnQuality);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetCodeTime(IntPtr objptr,
                                                         IntPtr pnTime);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncLoadRecipes(IntPtr objptr,
                                                  string cpFilePath,
                                                  string cpFilename);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncLoadRecipesToSlot(IntPtr objptr,
                                                        string cpFilePath,
                                                        string cpFilename,
                                                        int nSlot);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncSaveJob(IntPtr objptr,
                                                string cpFilePath,
                                                string cpFilename);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetLastError(IntPtr objptr);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetErrorDescription(IntPtr objptr,
                                                          int nError,
                                                          StringBuilder strText,
                                                          int nTextLength);

        // teachin functions
        [DllImport("wid110Lib.dll")]
        private static extern int FuncTeachingBCR(IntPtr objptr);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncTeachingOCR(IntPtr objptr);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncTeachingDMR(IntPtr objptr);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncConfigureBCR(IntPtr objptr, string cName);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncConfigureOCR(IntPtr objptr, string cName);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncConfigureDMR(IntPtr objptr, string cName);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncConfigureDelete(IntPtr objptr, string cName);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncConfigurePreset(IntPtr objptr, string cName);

        // image handling functions
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetImageRawData(IntPtr objptr, IntPtr pImageData);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetImageXSize(IntPtr objptr, IntPtr pXSize);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetImageYSize(IntPtr objptr, IntPtr pYSize);

        // further function for change configuration
        [DllImport("wid110Lib.dll")]
        private static extern int FuncSetFineScan(IntPtr objptr, IntPtr pMode);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetFineScan(IntPtr objptr, IntPtr pMode);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetFontName(IntPtr objptr, StringBuilder fName, int index);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetConfigurationName(IntPtr objptr, StringBuilder cName, int index);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncSaveJobToReader(IntPtr objptr);

        [DllImport("wid110Lib.dll")]
        private static extern int FuncGetROI(IntPtr objptr, string cName, IntPtr pWnd, IntPtr pCapture);
        [DllImport("wid110Lib.dll")]
        private static extern int FuncSetROI(IntPtr objptr, string cName, IntPtr pWnd, IntPtr pCapture);


        #endregion

        // ====================================================================
        /// <summary>
        /// Constructor: Initialize WID C# Library.
        /// </summary>
        // ====================================================================
        public Wid110Lib()
        {
            dll = FCreateDll();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        // ====================================================================
        /// <summary>
        /// Dispose: Free WID C# Library.
        /// </summary>
        // ====================================================================
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                FDestroyDll(dll);
                dll = IntPtr.Zero;
                disposedValue = true;
            }
        }

        // ====================================================================
        /// <summary>
        /// Finalizer: Free WID C# Library (Internal Implementation)
        /// </summary>
        // ====================================================================
        ~Wid110Lib()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        // ====================================================================
        /// <summary>
        /// Create WID C# Library instance.
        /// </summary>
        /// <return> DLL if created, NULL upon error.</return>
        // ====================================================================
        private IntPtr FCreateDll()
        {
            IntPtr d = IntPtr.Zero;

            lastExcp = "";
            errno = Wid110LibConst.ecNone;

            try
            {
                d = FuncCreateDll();
            }

            catch (Exception e)
            {
                lastExcp = e.ToString();
            }

            return d;
        }

        // ====================================================================
        /// <summary>
        /// Destroy WID C# Library instance.
        /// </summary>
        /// <param name="dll"> DLL to destroy.</param>
        /// <return>           true if destroyed.</return>
        // ====================================================================
        private bool FDestroyDll(IntPtr dll)
        {

            try
            {
                if (HandleDLLStatus(FuncDestroyDll(dll))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Initialize library and connect to IP.
        /// </summary>
        /// <param name="ip"> IP to connect.</param>
        /// <return>          true if connected.</return>
        // ====================================================================
        public bool FInit(string ip)
        {
            try
            {
                Ping PingHandler = new Ping();
                PingReply reply = PingHandler.Send(ip, 1000);
                if (reply.Status == IPStatus.Success)
                {
                    if (HandleDLLStatus(FuncInit(dll, ip))) return true;
                }
                else throw new IOException("IP Address " + ip + " not reachable!");
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Check library state.
        /// </summary>
        /// <return> true if initialized.</return>
        // ====================================================================
        public bool FIsInitialized()
        {
            try
            {
                if (HandleDLLStatus(FuncIsInitialized(dll)))
                    return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Check reader connection
        /// </summary>
        /// <param name="ip">Reader IP Address</param>
        /// <param name="timeout_s">Timeout in seconds</param>
        /// <return> true if connected.</return>
        // ====================================================================
        public bool FCheckConnection(string ip, int timeout_s)
        {
            try
            {
                if (HandleDLLStatus(FuncCheckConnection(dll, ip, timeout_s))) return true;

            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;

        }

        // ====================================================================
        /// <summary>
        /// Terminate connection.
        /// </summary>
        /// <return> true if disconnected.</return>
        // ====================================================================
        public bool FExit()
        {
            try
            {
                if (HandleDLLStatus(FuncExit(dll)))
                    return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Set the image acquisition parameter to the reader.
        /// </summary>
        /// <param name="capture">Image capturing parameters. <see cref="WID_CAPTURE"/></param>
        /// <returns>true if no error.</returns>
        // ====================================================================
        public bool FSetImageCapture(WID_CAPTURE capture)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);
            try
            {
                Marshal.StructureToPtr(capture, ptr, false);
                if (HandleDLLStatus(FuncSetImageCapture(dll, ptr))) return true;

            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get the image acquisition parameter from reader.
        /// </summary>
        /// <returns>Image capturing parameters. <see cref="WID_CAPTURE"/></returns>
        // ====================================================================
        public WID_CAPTURE FGetImageCapture()
        {
            WID_CAPTURE capture = new WID_CAPTURE();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);
            try
            {
                Marshal.StructureToPtr(capture, ptr, false);
                if (HandleDLLStatus(FuncGetImageCapture(dll, ptr)))
                {
                    capture = (WID_CAPTURE)Marshal.PtrToStructure(ptr, typeof(WID_CAPTURE));
                    return capture;
                }

            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return capture;
        }

        // ====================================================================
        /// <summary>
        /// Get library version.
        /// </summary>
        /// <param name="v"> WID110 library version.</param>
        // ====================================================================
        public string FGetVersion()
        {
            try
            {
                version = "";
                StringBuilder sb = new StringBuilder("", Wid110LibConst.versLenCS);
                if (HandleDLLStatus(FuncGetVersion(dll, sb, Wid110LibConst.versLenC)))
                {
                    version = sb.ToString();
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return version;
        }

        // ====================================================================
        /// <summary>
        /// Get sensor interface version.
        /// </summary>
        /// <return> WID110 interface version.</return>
        // ====================================================================
        public string FGetVersionParam()
        {
            try
            {
                versParam = "";
                StringBuilder sb = new StringBuilder("", Wid110LibConst.versLenCS);
                if (HandleDLLStatus(FuncGetVersionParam(dll, sb, Wid110LibConst.versLenC)))
                {
                    versParam = sb.ToString();
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return versParam;
        }

        // ====================================================================
        /// <summary>
        /// Change overlay flag.
        /// </summary>
        /// <param name="o"> overlay flag.</param>
        /// <return>         true if no error.</return>
        // ====================================================================
        public bool FSwitchOverlay(bool o)
        {
            try
            {
                if (HandleDLLStatus(FuncSwitchOverlay(dll, Convert.ToInt32(o)))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }

            return false;
        }

        // ====================================================================
        /// <summary>
        /// Perform a live read using the temporary settings from an
        /// earlier 'FLiveGetImage()' call.
        /// </summary>
        /// <return> true if done.</return>
        // ====================================================================
        public bool FLiveRead()
        {
            try
            {
                if (HandleDLLStatus(FuncLiveRead(dll))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Perform a live read using the given parameters, and decode.
        /// </summary>
        /// <param name="name">      name to save image to.</param>
        /// <param name="channel">   illumination channel.</param>
        /// <param name="intensity"> illumination intensity.</param>
        /// <param name="color">     illumination color.</param>
        /// <return>                 true if done.</return>
        // ====================================================================
        public bool FLiveGetImageRead(string name,
                               int channel,
                               int intensity,
                               int color)
        {
            try
            {
                if (HandleDLLStatus(FuncLiveGetImageRead(dll, name, channel, intensity, color))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Perform a process read..
        /// </summary>
        /// <return> true if done.</return>
        // ====================================================================
        public bool FProcessRead()
        {
            try
            {
                if (HandleDLLStatus(FuncProcessRead(dll))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get code quality for OCR codes.
        /// </summary>
        /// <return> code quality, or rsltNoCodeQuality upon failure.</return>
        // ====================================================================
        public int FGetCodeQualityOCR()
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(int));
            int rc = Wid110LibConst.rsltNoCodeQuality;

            try
            {
                if (HandleDLLStatus(FuncGetCodeQualityOCR(dll, q)))
                {
                    rc = Marshal.ReadInt32(q);
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return rc;
        }

        // ====================================================================
        /// <summary>
        /// Get code quality for BCR codes.
        /// </summary>
        /// <return> code quality, or rsltNoCodeQuality upon failure.</return>
        // ====================================================================
        public int FGetCodeQualityBCR()
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(int));
            int rc = Wid110LibConst.rsltNoCodeQuality;

            try
            {
                if (HandleDLLStatus(FuncGetCodeQualityBCR(dll, q)))
                {
                    rc = Marshal.ReadInt32(q);
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return rc;
        }

        // ====================================================================
        /// <summary>
        /// Get code quality for DMR codes.
        /// </summary>
        /// <return> code quality, or rsltNoCodeQuality upon failure.</return>
        // ====================================================================
        public int FGetCodeQualityDMR()
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(int));
            int rc = Wid110LibConst.rsltNoCodeQuality;

            try
            {
                if (HandleDLLStatus(FuncGetCodeQualityDMR(dll, q)))
                {
                    rc = Marshal.ReadInt32(q);
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return rc;
        }

        // ====================================================================
        /// <summary>
        /// Get code quality for LAST code.
        /// </summary>
        /// <return> code quality, or rsltNoCodeQuality upon failure.</return>
        // ====================================================================
        public int FGetCodeQualityLast()
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(int));
            int rc = Wid110LibConst.rsltNoCodeQuality;

            try
            {
                if (HandleDLLStatus(FuncGetCodeQualityLast(dll, q)))
                {
                    rc = Marshal.ReadInt32(q);
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return rc;
        }

        // ====================================================================
        /// <summary>
        /// Retrieve image from last process trigger.
        /// </summary>
        /// <param name="name"> file name to save image.</param>
        /// <param name="type"> 'pvImgBest' or 'pvImgAll'.</param>
        /// <return>            true if OK.</return>
        // ====================================================================
        public bool FProcessGetImage(string name,
                                      int type)
        {
            try
            {
                if (HandleDLLStatus(FuncProcessGetImage(dll, name, type))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get the last BCR/OCR/DMR decode result.
        /// </summary>
        /// <return> the latest read result.</return>
        // ====================================================================
        public string FGetWaferId()
        {
            StringBuilder sb = new StringBuilder(Wid110LibConst.rsltERROR, Wid110LibConst.rsltLenCS);
            string res = Wid110LibConst.rsltERROR;
            IntPtr pok = Marshal.AllocHGlobal(1);

            try
            {
                if (HandleDLLStatus(FuncGetWaferId(dll, sb, Wid110LibConst.rsltLenC, pok)))
                {
                    readOK = Marshal.ReadByte(pok, 0);

                    if (1 == readOK)
                        res = Wid110LibConst.rsltREAD + sb.ToString();
                    else
                        res = Wid110LibConst.rsltNOREAD + sb.ToString();

                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(pok);
            }
            return res;
        }

        // ====================================================================
        /// <summary>
        /// Take a single image using the given parameters.
        /// </summary>
        /// <param name="name">      name to save image to.</param>
        /// <param name="channel">   illumination channel.</param>
        /// <param name="intensity"> illumination intensity.</param>
        /// <param name="color">     illumination color.</param>
        /// <return>                 true if OK.</return>
        // ====================================================================
        public bool FLiveGetImage(string name,
                                   int channel,
                                   int intensity,
                                   int color)
        {
            try
            {
                if (HandleDLLStatus(FuncLiveGetImage(dll, name, channel, intensity, color))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Saves reader configuration into a *.wid file on the pc
        /// </summary>
        /// <param name="jobFilePath"> Full path to file.</param>
        /// <return>            true if OK.</return>
        // ====================================================================
        public bool FSaveJob(string jobFilePath)
        {
            string path = Path.GetDirectoryName(jobFilePath);
            string file = Path.GetFileName(jobFilePath);
            try
            {
                if (HandleDLLStatus(FuncSaveJob(dll, path, file))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Load process parameter file
        /// </summary>
        /// <param name="jobFilePath"> Full path of job file</param>
        /// <return>            true if OK.</return>
        // ====================================================================
        public bool FLoadRecipes(string jobFilePath)
        {
            try
            {
                if (!File.Exists(jobFilePath)) throw new FileNotFoundException(jobFilePath);
                string path = Path.GetDirectoryName(jobFilePath);
                string file = Path.GetFileName(jobFilePath);

                if (HandleDLLStatus(FuncLoadRecipes(dll, path, file))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Load parameters to a certain parameter slot (if applicable)
        /// </summary>
        /// <param name="jobFilePathpath"> Full path to job file.</param>
        /// <param name="slot"> slot (only for ocf and led files).</param>
        /// <return>            true if valid.</return>
        // ====================================================================
        public bool FLoadRecipesToSlot(string jobFilePath,
                                        int slot)
        {
            try
            {
                if (!File.Exists(jobFilePath)) throw new FileNotFoundException(jobFilePath);
                string path = Path.GetDirectoryName(jobFilePath);
                string file = Path.GetFileName(jobFilePath);

                if (HandleDLLStatus(FuncLoadRecipesToSlot(dll, path, file, slot))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Save Job to Reader
        /// </summary>
        /// <return>true if valid.</return>
        // ====================================================================
        public bool FSaveJobToReader()
        {
            try
            {
                if (HandleDLLStatus(FuncSaveJobToReader(dll))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get code time parameter
        /// </summary>
        /// <return> overall process time or rsltNoCodeTime upon failure.</return>
        // ====================================================================
        public int FGetCodeTime()
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(int));
            int rc = Wid110LibConst.rsltNoCodeTime;

            try
            {
                if (HandleDLLStatus(FuncGetCodeTime(dll, q)))
                {
                    rc = Marshal.ReadInt32(q);
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return rc;
        }

        // ====================================================================
        /// <summary>
        /// Get error description.
        /// </summary>
        /// <param name="eno"> error number to translate.</param>
        /// <return> error description.</return>
        // ====================================================================
        public string FGetErrorDescription(int eno)
        {
            StringBuilder sb = new StringBuilder(Wid110LibConst.errDESC, Wid110LibConst.errLen);
            int len = Wid110LibConst.errLen;

            try		// if lib handle is invalid, error text is generated as well
            {
                if (FuncGetErrorDescription(dll, eno, sb, len) == Wid110LibConst.rcNoError)
                    return sb.ToString();
            }

            catch (Exception e)
            {
                lastExcp = e.ToString();
            }
            return Wid110LibConst.errDESC;
        }

        // ====================================================================
        /// <summary>
        /// Get last error number.
        /// </summary>
        /// <return> last error number.</return>
        // ====================================================================
        public int FGetLastError()
        {
            try
            {
                return FuncGetLastError(dll);		// get last internal lib error
            }

            catch (Exception e)
            {
                lastExcp = e.ToString();
            }

            return errno;
        }

        // ====================================================================
        /// <summary>
        /// Handle error from Wid110.dll (Internal use only)
        /// </summary>
        /// <param name="status">return status from DLL Function</param>
        /// <returns></returns>
        // ====================================================================
        private bool HandleDLLStatus(int status)
        {
            lastExcp = "";

            switch (status)
            {
                case Wid110LibConst.rcError:
                    errno = FuncGetLastError(dll);
                    return false;

                case Wid110LibConst.rcInvObj:
                    errno = Wid110LibConst.ecInvObj;
                    return false;

                case Wid110LibConst.rcNoError:
                    errno = Wid110LibConst.ecNone;
                    return true;
            }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get the last error number.
        /// </summary>
        /// <return> last error number.</return>
        // ====================================================================
        public int getErrno()
        {
            return errno;
        }

        // ====================================================================
        /// <summary>
        /// Get the result read state.
        /// </summary>
        /// <return> result read state.</return>
        // ====================================================================
        public int getReadOK()
        {
            return readOK;
        }

        // ====================================================================
        /// <summary>
        /// Get the last exception message.
        /// </summary>
        /// <return> last exception message.</return>
        // ====================================================================
        public string getLastExcp()
        {
            return lastExcp;
        }

        // ====================================================================
        /// <summary>
        /// Get temporary image name.
        /// </summary>
        /// <return> temporary image name.</return>
        // ====================================================================
        public string getTmpImage()
        {
            return tmpImage;
        }

        // ====================================================================
        /// <summary>
        /// Get exception state.
        /// </summary>
        /// <return> true if exception.</return>
        // ====================================================================
        public bool isException()
        {
            return lastExcp.Length != 0;
        }

        // ====================================================================
        /// <summary>
        /// Check error for last function call
        /// </summary>
        /// <param name="errorMesage">Return error message from Wid110Lib.dll</param>
        /// <returns>True if error raised.</returns>
        // ====================================================================
        public bool CheckError(out string errorMesage)
        {
            errorMesage = string.Empty;
            if (isException())
            {
                errorMesage = getLastExcp();
                return true;
            }

            if (errno == Wid110LibConst.ecNone) return false;

            errorMesage = FGetErrorDescription(errno);
            return true;
        }

        // ====================================================================
        /// <summary>
        /// Return font name on selected index
        /// </summary>
        /// <param name="index">Font index. [0 - 7]</param>
        /// <returns>Font name</returns>
        // ====================================================================
        public string FGetFontName(int index)
        {
            try
            {
                StringBuilder sb = new StringBuilder("", 255);
                if (HandleDLLStatus(FuncGetFontName(dll, sb, index)))
                {
                    return sb.ToString();
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return string.Empty;
        }

        // ====================================================================
        /// <summary>
        /// Set fine scanning mode flag
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        // ====================================================================
        public bool FSetFineScan(bool value)
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(Int32));
            try
            {
                Marshal.WriteInt32(q, Convert.ToInt32(value));
                if (HandleDLLStatus(FuncSetFineScan(dll, q))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get fine scanning mode flag
        /// </summary>
        /// <returns>True if fine scanning enabled, return false if error</returns>
        // ====================================================================
        public bool FGetFineScan()
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(Int32));
            try
            {
                if (HandleDLLStatus(FuncGetFineScan(dll, q)))
                {
                    return Convert.ToBoolean(Marshal.ReadInt32(q));
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Retrieve name of saved configuration
        /// </summary>
        /// <param name="index">Slot index [1 -99]</param>
        /// <returns>Configuration Name</returns>
        // ====================================================================
        public string FGetConfigurationName(int index)
        {
            try
            {
                StringBuilder sb = new StringBuilder("", 255);
                if (HandleDLLStatus(FuncGetConfigurationName(dll, sb, index)))
                {
                    return sb.ToString();
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return string.Empty;
        }

        // ====================================================================
        /// <summary>
        /// Set OCR Parameter
        /// </summary>
        /// <param name="ocr">OCR Parameters</param>
        /// <param name="capture">Image Capture Parameters</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FSetParamOCR(WID_OCR ocr, WID_CAPTURE capture)
        {
            WID_OCR_Unsafe unsafeOcr = ocr.ToUnsafeStruct();
            IntPtr ptrOcr = Marshal.AllocHGlobal(Marshal.SizeOf(unsafeOcr) + 1);
            IntPtr ptrCapture = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);
            try
            {
                Marshal.StructureToPtr(capture, ptrCapture, false);
                Marshal.StructureToPtr(unsafeOcr, ptrOcr, false);
                if (HandleDLLStatus(FuncSetParamOCR(dll, ptrOcr, ptrCapture))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptrCapture);
                Marshal.FreeHGlobal(ptrOcr);
            }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get OCR Parameter
        /// </summary>
        /// <param name="ocr">OCR Parameters</param>
        /// <param name="capture">Image Capture Parameters</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FGetParamOCR(out WID_OCR ocr, out WID_CAPTURE capture)
        {
            WID_OCR_Unsafe unsafeOcr = new WID_OCR_Unsafe();
            IntPtr ptrOcr = Marshal.AllocHGlobal(Marshal.SizeOf(unsafeOcr) + 1);

            capture = new WID_CAPTURE();
            IntPtr ptrCapture = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);

            try
            {
                Marshal.StructureToPtr(unsafeOcr, ptrOcr, false);
                Marshal.StructureToPtr(capture, ptrCapture, false);

                if (HandleDLLStatus(FuncGetParamOCR(dll, ptrOcr, ptrCapture)))
                {
                    unsafeOcr = (WID_OCR_Unsafe)Marshal.PtrToStructure(ptrOcr, typeof(WID_OCR_Unsafe));
                    ocr = new WID_OCR(unsafeOcr);

                    capture = (WID_CAPTURE)Marshal.PtrToStructure(ptrCapture, typeof(WID_CAPTURE));
                    return true;
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptrOcr);
                Marshal.FreeHGlobal(ptrCapture);
            }

            ocr = new WID_OCR();
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Set BCR Parameter
        /// </summary>
        /// <param name="bcr">BCR Parameters</param>
        /// <param name="capture">Image Capture Parameters</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FSetParamBCR(WID_BCR bcr, WID_CAPTURE capture)
        {
            WID_BCR_Unsafe unsafeBcr = bcr.ToUnsafeStruct();
            IntPtr ptrBcr = Marshal.AllocHGlobal(Marshal.SizeOf(unsafeBcr) + 1);
            IntPtr ptrCapture = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);
            try
            {
                Marshal.StructureToPtr(capture, ptrCapture, false);
                Marshal.StructureToPtr(unsafeBcr, ptrBcr, false);
                if (HandleDLLStatus(FuncSetParamBCR(dll, ptrBcr, ptrCapture))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptrCapture);
                Marshal.FreeHGlobal(ptrBcr);
            }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get BCR Parameter
        /// </summary>
        /// <param name="bcr">BCR Parameters</param>
        /// <param name="capture">Image Capture Parameters</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FGetParamBCR(out WID_BCR bcr, out WID_CAPTURE capture)
        {
            WID_BCR_Unsafe unsafebcr = new WID_BCR_Unsafe();
            IntPtr ptrBcr = Marshal.AllocHGlobal(Marshal.SizeOf(unsafebcr) + 1);

            capture = new WID_CAPTURE();
            IntPtr ptrCapture = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);

            try
            {
                Marshal.StructureToPtr(unsafebcr, ptrBcr, false);
                Marshal.StructureToPtr(capture, ptrCapture, false);

                if (HandleDLLStatus(FuncGetParamBCR(dll, ptrBcr, ptrCapture)))
                {
                    unsafebcr = (WID_BCR_Unsafe)Marshal.PtrToStructure(ptrBcr, typeof(WID_BCR_Unsafe));
                    bcr = new WID_BCR(unsafebcr);

                    capture = (WID_CAPTURE)Marshal.PtrToStructure(ptrCapture, typeof(WID_CAPTURE));
                    return true;
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptrBcr);
                Marshal.FreeHGlobal(ptrCapture);
            }

            bcr = new WID_BCR();
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Set DMR Parameter
        /// </summary>
        /// <param name="dmr">DMR Parameters</param>
        /// <param name="capture">Image Capture Parameters</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FSetParamDMR(WID_DMR dmr, WID_CAPTURE capture)
        {
            WID_DMR_Unsafe usafeDmr = dmr.ToUnsafeStruct();
            IntPtr ptrDmr = Marshal.AllocHGlobal(Marshal.SizeOf(usafeDmr) + 1);
            IntPtr ptrCapture = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);
            try
            {
                Marshal.StructureToPtr(capture, ptrCapture, false);
                Marshal.StructureToPtr(usafeDmr, ptrDmr, false);
                if (HandleDLLStatus(FuncSetParamDMR(dll, ptrDmr, ptrCapture))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptrCapture);
                Marshal.FreeHGlobal(ptrDmr);
            }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get DMR Parameter
        /// </summary>
        /// <param name="dmr">DMR Parameters</param>
        /// <param name="capture">Image Capture Parameters</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FGetParamDMR(out WID_DMR dmr, out WID_CAPTURE capture)
        {
            WID_DMR_Unsafe unsafedmr = new WID_DMR_Unsafe();
            IntPtr ptrDmr = Marshal.AllocHGlobal(Marshal.SizeOf(unsafedmr) + 1);

            capture = new WID_CAPTURE();
            IntPtr ptrCapture = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);

            try
            {
                Marshal.StructureToPtr(unsafedmr, ptrDmr, false);
                Marshal.StructureToPtr(capture, ptrCapture, false);

                if (HandleDLLStatus(FuncGetParamDMR(dll, ptrDmr, ptrCapture)))
                {
                    unsafedmr = (WID_DMR_Unsafe)Marshal.PtrToStructure(ptrDmr, typeof(WID_DMR_Unsafe));
                    dmr = new WID_DMR(unsafedmr);

                    capture = (WID_CAPTURE)Marshal.PtrToStructure(ptrCapture, typeof(WID_CAPTURE));
                    return true;
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptrDmr);
                Marshal.FreeHGlobal(ptrCapture);
            }

            dmr = new WID_DMR();
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Set ROI of named configuration
        /// </summary>
        /// <param name="configName">Configuration name</param>
        /// <param name="roi">ROI Parameters</param>
        /// <param name="capture">Image Capture Parameters</param>
        /// <returns>true if no error.</returns>
        // ====================================================================
        public bool FSetROI(string configName, WID_ROI roi, WID_CAPTURE capture)
        {
            IntPtr ptrRoi = Marshal.AllocHGlobal(Marshal.SizeOf(roi) + 1);
            IntPtr ptrCapture = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);
            try
            {
                Marshal.StructureToPtr(roi, ptrRoi, false);
                Marshal.StructureToPtr(capture, ptrCapture, false);
                if (HandleDLLStatus(FuncSetROI(dll, configName, ptrRoi, ptrCapture))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptrCapture);
                Marshal.FreeHGlobal(ptrRoi);
            }
            return false;

        }

        // ====================================================================
        /// <summary>
        /// Get ROI of named configuration
        /// </summary>
        /// <param name="configName">Configuration name</param>
        /// <param name="roi">ROI Parameters</param>
        /// <param name="capture">Image Capture Parameters</param>
        /// <returns>true if no error.</returns>
        // ====================================================================
        public bool FGetROI(string configName, out WID_ROI roi, out WID_CAPTURE capture)
        {
            roi = new WID_ROI();
            IntPtr ptrRoi = Marshal.AllocHGlobal(Marshal.SizeOf(roi) + 1);

            capture = new WID_CAPTURE();
            IntPtr ptrCapture = Marshal.AllocHGlobal(Marshal.SizeOf(capture) + 1);

            try
            {
                Marshal.StructureToPtr(roi, ptrRoi, false);
                Marshal.StructureToPtr(capture, ptrCapture, false);

                if (HandleDLLStatus(FuncGetROI(dll, configName, ptrRoi, ptrCapture)))
                {
                    roi = (WID_ROI)Marshal.PtrToStructure(ptrRoi, typeof(WID_ROI));
                    capture = (WID_CAPTURE)Marshal.PtrToStructure(ptrCapture, typeof(WID_CAPTURE));
                    return true;
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally
            {
                Marshal.FreeHGlobal(ptrRoi);
                Marshal.FreeHGlobal(ptrCapture);
            }

            return false;
        }

        // ====================================================================
        /// <summary>
        /// Get Image Width
        /// </summary>
        /// <returns>Image Width in pixels</returns>
        // ====================================================================
        public int FGetImageXSize()
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(Int32));
            try
            {
                if (HandleDLLStatus(FuncGetImageXSize(dll, q)))
                {
                    return Convert.ToInt32(Marshal.ReadInt32(q));
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return -1;
        }

        // ====================================================================
        /// <summary>
        /// Get Image Height
        /// </summary>
        /// <returns>Image height in pixels</returns>
        // ====================================================================
        public int FGetImageYSize()
        {
            IntPtr q = Marshal.AllocHGlobal(sizeof(Int32));
            try
            {
                if (HandleDLLStatus(FuncGetImageYSize(dll, q)))
                {
                    return Convert.ToInt32(Marshal.ReadInt32(q));
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(q); }
            return -1;

        }

        // ====================================================================
        /// <summary>
        /// Delete all configuration with the name.
        /// </summary>
        /// <param name="configName">Configuration Name</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FConfigureDelete(string configName)
        {
            try
            {
                if (HandleDLLStatus(FuncConfigureDelete(dll, configName))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Preset Configuration with matching name.
        /// </summary>
        /// <param name="configName">Configuration name, leave empty to reset cycle</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FConfigurePreset(string configName = "")
        {
            try
            {
                if (HandleDLLStatus(FuncConfigurePreset(dll, configName))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Tune OCR
        /// </summary>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FTeachingOCR()
        {
            try
            {
                if (HandleDLLStatus(FuncTeachingOCR(dll))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Tune Barcode (NOT IMPLEMENTED YET)
        /// </summary>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FTeachingBCR()
        {
            try
            {
                if (HandleDLLStatus(FuncTeachingBCR(dll))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Tune 2D Barcode (NOT IMPLEMENTED YET)
        /// </summary>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FTeachingDMR()
        {
            try
            {
                if (HandleDLLStatus(FuncTeachingDMR(dll))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// OCR Configuration steps
        /// </summary>
        /// <param name="configName">null or configuration name</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FConfigureOCR(string configName = "\0")
        {
            try
            {
                if (HandleDLLStatus(FuncConfigureOCR(dll, configName))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// Barcode Configuration steps. (NOT IMPLEMENTED YET)
        /// </summary>
        /// <param name="configName">null or configuration name</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FConfigureBCR(string configName = "")
        {
            try
            {
                if (HandleDLLStatus(FuncConfigureBCR(dll, configName))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        // ====================================================================
        /// <summary>
        /// 2D Barcode Configuration steps. (NOT IMPLEMENTED YET)
        /// </summary>
        /// <param name="configName">null or configuration name</param>
        /// <returns>true if no error</returns>
        // ====================================================================
        public bool FConfigureDMR(string configName = "")
        {
            try
            {
                if (HandleDLLStatus(FuncConfigureDMR(dll, configName))) return true;
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            return false;
        }

        public bool FGetImageRawData(out Image image)
        {
            image = null;
            int width = FGetImageXSize();
            int height = FGetImageYSize();

            int imageSize = width * height;
            byte[] imageData = new byte[imageSize];

            IntPtr ptrImage = Marshal.AllocHGlobal(imageData.Length);

            try
            {
                if (HandleDLLStatus(FuncGetImageRawData(dll, ptrImage)))
                {
                    Marshal.Copy(ptrImage, imageData, 0, imageSize);
                    byte[] flippedData = ImageDataFlipHorizontal(imageData, width, height);
                    image = CreateBMP(width, height, flippedData);
                    image.Save("BMPDump.bmp");
                    image.Dispose();
                    return true;
                }
            }
            catch (Exception e) { lastExcp = e.ToString(); }
            finally { Marshal.FreeHGlobal(ptrImage); }
            return false;
        }

        private byte[] ImageDataFlipHorizontal(byte[] i, int w, int h)
        {
            byte[] d = new byte[i.Length];

            for (int r = 0; r < h; r++)
            {
                Array.Copy(i, r * w, d, (h - 1 - r) * w, w);
            }
            return d;
        }


        private Bitmap CreateBMP(int width, int height, byte[] values)
        {
            Bitmap b = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            ColorPalette ncp = b.Palette;
            for (int i = 0; i < 256; i++)
                ncp.Entries[i] = Color.FromArgb(255, i, i, i);
            b.Palette = ncp;

            var BoundsRect = new Rectangle(0, 0, width, height);
            BitmapData bmpData = b.LockBits(BoundsRect,
                                            ImageLockMode.WriteOnly,
                                            b.PixelFormat);

            IntPtr ptr = bmpData.Scan0;

            int bytes = bmpData.Stride * b.Height;


            Marshal.Copy(values, 0, ptr, bytes);
            b.UnlockBits(bmpData);
            return b;
        }

    }
}
