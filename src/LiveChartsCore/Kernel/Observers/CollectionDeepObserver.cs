// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
#if NET462
using System.Linq;
#endif

namespace LiveChartsCore.Kernel.Observers;

/// <summary>
/// A Class that tracks both, <see cref="INotifyCollectionChanged.CollectionChanged"/> event and 
/// the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of each element in the collection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CollectionDeepObserver"/> class.
/// </remarks>
/// <param name="onChange">
/// An action that is called when the collection items or a property in an item in the collection change.
/// </param>
/// <param name="onItemAdded">
/// if specified, this action is called for each new item in the collection.
/// This action is also called for each item when the collection is initialized.
/// </param>
/// <param name="onItemRemoved">
/// if specified, this acction is called each time an item is removed in the collection.
/// This action is also called for each item when the collection is disposed.
/// </param>
public class CollectionDeepObserver(
    Action onChange,
    Action<object>? onItemAdded = null,
    Action<object>? onItemRemoved = null)
        : IObserver
{
    private readonly HashSet<INotifyPropertyChanged> _itemsListening = [];
    private IEnumerable? _trackedCollection;
    private bool _walksItems;

    /// <inheritdoc cref="IObserver.Initialize(object?)"/>
    public void Initialize(object? instance)
    {
        if (_trackedCollection == instance)
            return;

        if (_trackedCollection is not null)
            Dispose();

        if (instance is not IEnumerable enumerable) return;

        if (instance is INotifyCollectionChanged incc)
            incc.CollectionChanged += OnCollectionChanged;

        // The per-item walk exists to subscribe INotifyPropertyChanged items and to raise
        // the per-item callbacks. When neither can ever apply — no callbacks registered
        // and the element type provably has no trackable instances — skip it entirely:
        // the walk is O(N) and boxes every struct, which at large-data scale (a multi-
        // million-point double[] or struct[] assigned to Series.Values) measures in
        // SECONDS of pure overhead on every assignment.
        _walksItems = onItemAdded is not null || onItemRemoved is not null || MayContainTrackableItems(enumerable);
        if (_walksItems)
            OnItemsAdded(enumerable);

        _trackedCollection = enumerable;
    }

    /// <inheritdoc cref="IObserver.Dispose"/>
    public void Dispose()
    {
        if (_trackedCollection is null) return;

        if (_trackedCollection is INotifyCollectionChanged incc)
            incc.CollectionChanged -= OnCollectionChanged;

        if (_walksItems)
            OnItemsRemoved(_trackedCollection);

        _trackedCollection = null;
    }

    // Whether any instance in the collection could ever need PropertyChanged tracking,
    // decided from the collection's IEnumerable<T> element type(s):
    //
    //   - A VALUE TYPE never can — even one implementing INPC: enumerating through the
    //     non-generic IEnumerable boxes each item, so the subscription would attach to a
    //     temporary box the collection does not hold (the handler can never fire) while
    //     _itemsListening roots that box. Skipping is both faster and more correct.
    //   - A SEALED reference type that does not implement INPC has no INPC instances.
    //   - Anything else (an INPC type, an open hierarchy, a non-generic collection)
    //     may contain trackable items — walk as always.
    private static bool MayContainTrackableItems(IEnumerable enumerable)
    {
        var foundElementType = false;

        foreach (var i in enumerable.GetType().GetInterfaces())
        {
            if (!i.IsGenericType || i.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                continue;

            var elementType = i.GetGenericArguments()[0];

            if (elementType.IsValueType ||
                (elementType.IsSealed && !typeof(INotifyPropertyChanged).IsAssignableFrom(elementType)))
            {
                foundElementType = true;
                continue;
            }

            return true;
        }

        // Walk unless every element type was provably untrackable (a non-generic
        // collection exposes no element type to prove anything about).
        return !foundElementType;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_walksItems)
        {
            // Untrackable items and no per-item callbacks: any change is just "redraw".
            onChange();
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:

                if (e.NewItems is not null)
                    OnItemsAdded(e.NewItems);

                break;

            case NotifyCollectionChangedAction.Remove:

                if (e.OldItems is not null)
                    OnItemsRemoved(e.OldItems);

                break;

            case NotifyCollectionChangedAction.Replace:

                if (e.OldItems is not null)
                    OnItemsRemoved(e.OldItems);

                if (e.NewItems is not null)
                    OnItemsAdded(e.NewItems);

                break;

            case NotifyCollectionChangedAction.Reset:

#if NET462
                OnItemsRemoved(_itemsListening.ToArray());
#else
                OnItemsRemoved(_itemsListening);
#endif
                _itemsListening.Clear();

                if (sender is IEnumerable enumerable)
                    OnItemsAdded(enumerable);

                break;

            case NotifyCollectionChangedAction.Move:
            default:

                break;
        }

        onChange();
    }

    private void OnItemsAdded(IEnumerable newItems)
    {
        foreach (var item in newItems)
        {
            if (item is INotifyPropertyChanged inpcItem)
            {
                if (_itemsListening.Add(inpcItem))
                {
                    inpcItem.PropertyChanged += OnItemPropertyChanged;
                }
            }

            onItemAdded?.Invoke(item);
        }
    }

    private void OnItemsRemoved(IEnumerable oldItems)
    {
        foreach (var item in oldItems)
        {
            if (item is INotifyPropertyChanged inpcItem && _itemsListening.Remove(inpcItem))
            {
                inpcItem.PropertyChanged -= OnItemPropertyChanged;
            }

            onItemRemoved?.Invoke(item);
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) => onChange();
}
