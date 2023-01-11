using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

public class WeightCounter
{
    private List<float> _weightSumed;
    private int _count = 0;
    public int Count { get => _count; }
    public void Add(List<float> weight)
    {
        if (_weightSumed != null && _weightSumed.Count != weight.Count)
        {
            throw new Exception("_weightSumed and weight must have save count");
        }
        // first time
        if (_weightSumed == null)
        {
            _weightSumed = weight;
            _count = 1;
        }
        else
        {
            for (int i = 0; i < _weightSumed.Count; i++)
                _weightSumed[i] += weight[i];
            _count++;
        }
    }
    public List<float> Average()
    {
        if (_weightSumed != null)
            for (int i = 0; i < _weightSumed.Count; i++)
                _weightSumed[i] /= _count;
        return _weightSumed;
    }

}
