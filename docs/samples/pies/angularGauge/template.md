{{ render this "~/shared/genericSampleJustGifHeader.md" }}

#### View model

```
{{ full_name | get_vm_from_docs }}
```

## {{~ view_title ~}}

```
{{ full_name | get_view_from_docs }}
```

# Custom needle

You can inherit from `NeedleGeometry` to change the aspect of the needle, for example in the next code snippet, 
the `SmallNeedle` class inherits from `NeedleGeometry`, then in the constructor it sets the `ScaleTransform`
property to `0.6` in the `X` and `Y` axis, this will make the needle 40% smaller.

{{~ render_params_file_as_code this "~/../samples/ViewModelsSamples/Pies/AngularGauge/SmallNeedle.cs" ~}}

Finally we need to use this new needle in our gauge, in the example above change the type `NeedleVisual`
to `NeedleVisual<SmallNeedle>`.

```csharp
public partial class ViewModel
{
    // ...

    public NeedleVisual<SmallNeedle> Needle { get; set; }

    public ViewModel()
    {
        Needle = new NeedleVisual<SmallNeedle>
        {
            Value = 45
        };
        
        // ...
    }
    
    // ...
}
```

Run the app again, now the needle is 40% smaller.

You can also override the `Draw()` method and use SkiaSharp to create your own needle, in the next snippet,
we are drawing a rectangle using SkiaSharp to represent the needle:

{{~ render_params_file_as_code this "~/../samples/ViewModelsSamples/Pies/AngularGauge/CustomNeedle.cs" ~}}

Finally we need to use this new needle in our gauge:"

```csharp
public partial class ViewModel
{
    // ...

    public NeedleVisual<CustomNeedle> Needle { get; set; }

    public ViewModel()
    {
        Needle = new NeedleVisual<CustomNeedle>
        {
            Value = 45
        };
        
        // ...
    }
    
    // ...
}
```

Run the app again, now there is a rectangle as our needle:

<div class="text-center sample-img">
    <img src="{{ assets_url }}/docs/{{ unique_name }}/needle-rect.png" alt="sample image" />
</div>

:::tip
You can draw anything with SkiaSharp, this article does not explain how to do it.
If you need help, you can see the default [NeedleGeometry source code](https://github.com/beto-rodriguez/LiveCharts2/blob/master/src/skiasharp/LiveChartsCore.SkiaSharp/Drawing/Geometries/NeedleGeometry.cs), or you can follow any SkiaSharp guide.
:::

{{ render this "~/shared/relatedTo.md" }}