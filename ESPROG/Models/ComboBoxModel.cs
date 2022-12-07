namespace ESPROG.Models
{
    internal class ComboBoxModel<T1, T2>
    {
        public T1 Display { get; private set; }
        public T2 Value { get; private set; }

        public ComboBoxModel(T1 display, T2 value)
        {
            Display = display;
            Value = value;
        }
    }
}
