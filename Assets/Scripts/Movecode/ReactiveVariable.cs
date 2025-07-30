using System;

public class ReactiveVariable<T> where T : IComparable<T>
{
    public Action<T, T> OnValueChanged;

    private T value;

    public ReactiveVariable(T value)
    {
        this.value = value;
    }

    public ReactiveVariable() : this(default(T))
    {

    }

    public T Value
    {
        get => value;
        set
        {
            T oldValue = this.value;

            this.value = value;

            if (value.CompareTo(oldValue) != 0)
            {
                OnValueChanged?.Invoke(oldValue, this.value);
            }
        }
    }
}
