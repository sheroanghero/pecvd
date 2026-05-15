//---------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
//---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using EGC = ExtendedGrid.Microsoft.Windows.Controls;

using MS.Internal;

namespace ExtendedGrid.Microsoft.Windows.Controls
{
    /// <summary>
    /// The separator used to indicate drop location during column header drag-drop
    /// </summary>
    internal class DataGridColumnDropSeparator : Separator
    {
        #region Constructors

        static DataGridColumnDropSeparator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(EGC.DataGridColumnDropSeparator),
                new FrameworkPropertyMetadata(typeof(EGC.DataGridColumnDropSeparator)));

            WidthProperty.OverrideMetadata(
                typeof(EGC.DataGridColumnDropSeparator), 
                new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceWidth)));
            
            HeightProperty.OverrideMetadata(
                typeof(EGC.DataGridColumnDropSeparator), 
                new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceHeight)));
        }

        #endregion

        #region Static Methods

        private static object OnCoerceWidth(DependencyObject d, object baseValue)
        {
            double width = (double)baseValue;
            if (DoubleUtil.IsNaN(width))
            {
                return 2.0;
            }

            return baseValue;
        }

        private static object OnCoerceHeight(DependencyObject d, object baseValue)
        {
            double height = (double)baseValue;
            EGC.DataGridColumnDropSeparator separator = (EGC.DataGridColumnDropSeparator)d;
            if (separator._referenceHeader != null && DoubleUtil.IsNaN(height))
            {
                return separator._referenceHeader.ActualHeight;
            }

            return baseValue;
        }

        #endregion

        #region Properties

        internal EGC.DataGridColumnHeader ReferenceHeader
        {
            get
            {
                return _referenceHeader;
            }

            set
            {
                _referenceHeader = value;
            }
        }

        #endregion

        #region Data

        private EGC.DataGridColumnHeader _referenceHeader;

        #endregion
    }
}
