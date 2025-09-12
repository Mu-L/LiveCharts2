﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;

namespace WinFormsSample.General.MultiThreading;

public partial class View : UserControl
{
    private readonly ObservableCollection<int> _values;
    private readonly object _sync = new();
    private int _current;
    private bool _isReading = true;

    public View()
    {
        InitializeComponent();
        Size = new System.Drawing.Size(600, 400);

        // Create initial data
        var items = new List<int>();
        for (var i = 0; i < 1500; i++)
        {
            _current += new Random().Next(-9, 10);
            items.Add(_current);
        }
        _values = new ObservableCollection<int>(items);

        var cartesianChart = new CartesianChart
        {
            SyncContext = _sync, // Set the sync context to allow thread-safe updates
            Series = [
                new LineSeries<int>
                {
                    Values = _values,
                    GeometryFill = null,
                    GeometryStroke = null
                }
            ],
            Location = new System.Drawing.Point(0, 0),
            Size = new System.Drawing.Size(600, 400),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
        };

        Controls.Add(cartesianChart);

        // Start background tasks to update data
        for (var i = 0; i < 10; i++)
        {
            _ = Task.Run(ReadData);
        }
    }

    private async Task ReadData()
    {
        var r = new Random();
        await Task.Delay(1000);
        while (_isReading)
        {
            await Task.Delay(1);
            _current = Interlocked.Add(ref _current, r.Next(-9, 10));

            // Add the new value to the collection
            // we use a lock to ensure thread safety
            lock (_sync)
            {
                if (_values.Count > 0)
                {
                    _values.Add(_current);
                    _values.RemoveAt(0);
                }
            }
        }
    }
}
