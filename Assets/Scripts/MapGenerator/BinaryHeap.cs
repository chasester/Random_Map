using System;
using System.Collections.Generic;

/*
 * Code Inspired from KooKoo on Stackexchange
 * https://codereview.stackexchange.com/questions/84530/simple-binary-heap-in-c
 */

public class BinaryHeap<T> //T is the type of object you are making
{
    private List<T> items;
    public delegate bool Comparator(T a,T b);
    private Comparator comparator;

    public T Root
    {
        get { return items[0]; }
    }

    public BinaryHeap(Comparator c) //c is a function where 2 objects are passed in and if the first object is greatrothan (or less than)
    {
        items = new List<T>();
        comparator = new Comparator(c);
    }

    public void Insert(T item)
    {
        items.Add(item);

        int i = items.Count - 1;

        while (i > 0)
        {
            if (comparator.Invoke(items[i],items[(i - 1) / 2]))
            {
                T temp = items[i];
                items[i] = items[(i - 1) / 2];
                items[(i - 1) / 2] = temp;
                i = (i - 1) / 2;
            }
            else
                break;
        }
    }

    private void DeleteRoot()
    {
        int i = items.Count - 1;

        items[0] = items[i];
        items.RemoveAt(i);

        i = 0;

        while (true)
        {
            int leftInd = 2 * i + 1;
            int rightInd = 2 * i + 2;
            int largest = i;

            if (leftInd < items.Count)
            {
                if (comparator.Invoke(items[leftInd],items[largest]))
                    largest = leftInd;
            }

            if (rightInd < items.Count)
            {
                if (comparator.Invoke(items[rightInd],items[largest]))
                    largest = rightInd;
            }

            if (largest != i)
            {
                T temp = items[largest];
                items[largest] = items[i];
                items[i] = temp;
                i = largest;
            }
            else
                break;
        }
    }

    public T PopRoot()
    {
        T result = items[0];

        DeleteRoot();

        return result;
    }
}
