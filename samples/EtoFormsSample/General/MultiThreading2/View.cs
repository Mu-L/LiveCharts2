﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Eto.Forms;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Eto;

namespace EtoFormsSample.General.MultiThreading2;

public class View : Panel
{
    private readonly ObservableCollection<int> _values;
    private int _current;
    private bool _isReading = true;
    private readonly CartesianChart cartesianChart;

    public View()
    {
        var items = new List<int>();
        for (var i = 0; i < 1500; i++)
        {
            _current += new Random().Next(-9, 10);
            items.Add(_current);
        }
        _values = new ObservableCollection<int>(items);

        cartesianChart = new CartesianChart
        {
            Series =
            [
                new LineSeries<int>
                {
                    Values = _values,
                    GeometryFill = null,
                    GeometryStroke = null,
                }
            ]
        };

        Content = cartesianChart;

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
            Application.Instance.InvokeAsync(() => UpdateValues(r));
        }
    }

    private void UpdateValues(Random r)
    {
        _current += r.Next(-9, 10);
        _values.Add(_current);
        _values.RemoveAt(0);
    }
}
