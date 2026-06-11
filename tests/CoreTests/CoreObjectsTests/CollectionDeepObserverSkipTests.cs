using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CoreTests.MockedObjects;
using LiveChartsCore.Kernel.Observers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.CoreObjectsTests;

// The observer's per-item walk (subscribe INotifyPropertyChanged on every item) is
// skipped when no instance could ever need it: value-type elements (enumeration boxes
// each item, so a subscription would attach to a temporary box the collection does not
// hold) and sealed non-INPC elements. Collection-level notifications still redraw, and
// per-item callbacks (the chart-level Series/Axes observers) always force the walk.
[TestClass]
public class CollectionDeepObserverSkipTests
{
    private struct InpcValue : INotifyPropertyChanged
    {
        public static int Subscriptions;

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => Subscriptions++;
            remove { }
        }
    }

    [TestMethod]
    public void InpcStructItems_AreNotSubscribed()
    {
        // Subscribing a struct's PropertyChanged through the walk attaches the handler to
        // the enumeration's TEMPORARY BOX — it can never fire, and the box stays rooted
        // in the observer. The walk must be skipped for value types entirely.
        InpcValue.Subscriptions = 0;
        var collection = new ObservableCollection<InpcValue> { new(), new() };

        var observer = new TestObserver<InpcValue>
        {
            MyCollection = collection
        };

        Assert.AreEqual(0, InpcValue.Subscriptions, "a boxed-copy subscription can never fire — it must not be made");

        collection.Add(new InpcValue());
        Assert.AreEqual(1, observer.ChangesCount, "collection changes still redraw");
        Assert.AreEqual(0, InpcValue.Subscriptions, "per-change items are not subscribed either");
    }

    [TestMethod]
    public void ValueTypeItems_StillRedrawOnEveryCollectionChange()
    {
        var collection = new ObservableCollection<int> { 1, 2, 3 };
        var observer = new TestObserver<int>
        {
            MyCollection = collection
        };

        collection.Add(4);
        Assert.AreEqual(1, observer.ChangesCount, "Add redraws");

        collection.RemoveAt(0);
        Assert.AreEqual(2, observer.ChangesCount, "Remove redraws");

        collection.Clear();
        Assert.AreEqual(3, observer.ChangesCount, "Reset redraws");

        observer.Dispose(); // the skipped walk must not break the symmetric dispose
    }

    [TestMethod]
    public void PerItemCallbacks_ForceTheWalk()
    {
        // The chart-level observers (Series/Axes/Sections) rely on per-item add/remove
        // callbacks — those must keep walking even for value-type elements.
        var seen = new List<object>();
        var observer = new CollectionDeepObserver(() => { }, onItemAdded: seen.Add);

        var collection = new ObservableCollection<int> { 1, 2, 3 };
        observer.Initialize(collection);
        Assert.AreEqual(3, seen.Count, "initialization raises the callback per existing item");

        collection.Add(4);
        Assert.AreEqual(4, seen.Count, "changes raise the callback per added item");

        observer.Dispose();
    }
}
