// ============================================================================
// Copyright (c) 2010  IOSS GmbH
// All Rights Reserved.
// ============================================================================


// ============================================================================
//
//      Wid110LibConst  -  WID110 C# based library constants
//
// ============================================================================
//
//      File:     Wid110LibConst.cs          Type:         Implementation
//
//      Date:     09.02.2010                 Last Change:  09.11.2011
//
//      Author:   Thomas M. Schlageter
//                Silvio Robel
//
//      Methods:  none
//
// ============================================================================


using System;
using System.Collections.Generic;
using System.Linq;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.OcrReaders.IOSS
{
    static partial class Wid110LibConstV7
    {
        // ====================================================================
        /// <summary>
        /// Constants that should be used overall WID110 library software.
        /// </summary>
        // ====================================================================

        // local system resources
        //
        public static string tmpImage = "C:\\testImage_WidLib.bmp";


        // return values: these must correspond with 'WID110Dll.h'
        // these are return values only for c-function calls
        //
        // function executed sucessfully
        public const int rcNoError = 1;	// RETVAL_NO_ERROR

        // function failed, calls to FGetLastError()
        // and FGetErrorDescription() possible
        public const int rcError = 0;	// RETVAL_ERROR

        // function parameter contained invalid lib handle, call
        // to FGetErrorDescription() possible  
        public const int rcInvObj = -1;	// RETVAL_INV_OBJ


        // error codes: these must correspond with 'WID110Dll.h'
        // these are values for internal lib errors returned by FGetLastError() 
        // an error description can be retrieved by calling FGetErrorDescription()
        //
        public static int ecInvObj = -1;	// RETVAL_INV_OBJ
        public static int ecNone = 0;	// ERROR_NONE
        public static int ecNotInit = 1;	// ERROR_NOT_INIT
        public static int ecNotFound = 2;	// ERROR_READER_NOTFOUND
        public static int ecNetInit = 3;	// ERROR_NETWORK_INIT
        public static int ecNetIP = 4;	// ERROR_NETWORK_IP
        public static int ecNetSend = 5;	// ERROR_NETWORK_SEND
        public static int ecNetRecv = 6;	// ERROR_NETWORK_RECEIVE
        public static int ecFileName = 7;	// ERROR_FILENAME
        public static int ecImgSave = 8;	// ERROR_IMG_SAVE
        public static int ecParLoad = 9;	// ERROR_PARAM_LOAD
        public static int ecNoProcTrg = 10;	// ERROR_NO_PROC_TRIGGERSTR
        public static int ecArgBufSz = 11;	// ERROR_ARGBUFFERSIZE
        public static int ecNoFailSt = 12;	// ERROR_NO_FAILSTR
        public static int ecNoMoreImg = 13;	// ERROR_NO_MORE_IMAGES
        public static int ecNoResult = 14;	// ERROR_NO_VALID_RESULT
        public static int ecNoImage = 15;	// ERROR_NO_VALID_IMAGE
        public static int ecNoTrgStr = 16;	// ERROR_NO_TRIGGERSTRING
        public static int ecNoVersPar = 17;	// ERROR_VERSION_PARAMETERSET
        public static int ecPOutOfRng = 18;	// ERROR_PARAMETER_OUTOFRANGE
        public static int ecNetTrig = 19;	// ERROR_NET_TRIGGERING


        // parameter values for FProcessGetImage(type)
        // these must correspond with 'WID110Dll.h'
        //
        public static int pvImgBest = 0;	// IMG_PROCESS_BEST
        public static int pvImgAll = 1;	// IMG_PROCESS_ALL


        // dummy results
        //
        public static string rsltFAIL = "fail";
        public static string rsltOK = "OK";
        public static string rsltERROR = "no read result";
        public static string rsltBLANK = " ";
        public static string rsltREAD = "READ: ";
        public static string rsltNOREAD = "NOREAD: ";


        // return value for FGetCodeQualityX()
        // if there is no quality retrieved, call GetLastError() then    
        public static int rsltNoCodeQuality = -1;

        // return value for FGetCodeTime()
        // if there is no time retrieved, call GetLastError() then    
        public static int rsltNoCodeTime = -1;


        // dummy error messages
        //
        public static string errNO = "no error";
        public static string errDESC = "ERROR; no error description";


        // string buffer sizes
        //
        public static int errLen = 256;

        public static int versLenCS = 63;
        public static int versLenC = versLenCS + 1;

        public static int rsltLenCS = 259;
        public static int rsltLenC = rsltLenCS + 1;
    }
}

