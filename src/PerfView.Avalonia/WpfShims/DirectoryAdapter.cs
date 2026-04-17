using Avalonia.Controls;
using System.Collections;
using System.Collections.ObjectModel;

namespace PerfView;

/// <summary>
/// Provides a HistoryComboBox-compatible API using a TextBox + ListBox + Popup,
/// working around Avalonia 12's ComboBox not rendering IsEditable mode properly.
/// </summary>
public class DirectoryAdapter
{
    private readonly TextBox m_textBox;
    private readonly ListBox m_listBox;
    private readonly ObservableCollection<string> m_items = new();

    public DirectoryAdapter(TextBox textBox, ListBox listBox)
    {
        m_textBox = textBox;
        m_listBox = listBox;
        m_listBox.ItemsSource = m_items;
    }

    public int HistoryLength { get; set; } = 10;

    public string Text
    {
        get => m_textBox.Text ?? "";
        set => m_textBox.Text = value;
    }

    public ObservableCollection<string> Items => m_items;

    public void SetHistory(IEnumerable values)
    {
        m_items.Clear();
        int count = 0;
        foreach (var value in values)
        {
            count++;
            if (count >= HistoryLength)
                break;
            m_items.Add(value?.ToString() ?? "");
        }
    }

    public bool AddToHistory(string value)
    {
        if (m_items.Count > 0 && m_items[0] == value)
            return false;

        RemoveFromHistory(value);
        m_items.Insert(0, value);
        Text = value;

        while (m_items.Count > HistoryLength)
            m_items.RemoveAt(HistoryLength);

        return true;
    }

    public void RemoveFromHistory(string value)
    {
        var text = Text;
        for (int i = 0; i < m_items.Count; i++)
        {
            if (m_items[i] == value)
            {
                m_items.RemoveAt(i);
                Text = text;
                break;
            }
        }
    }

    public void Focus() => m_textBox.Focus();
}
