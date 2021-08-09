using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BufferSorter;
using System.Linq;

public class MergeSortTest : MonoBehaviour
{
    [SerializeField]
    private ComputeShader m_mergeSortComputeShader;
    
    [SerializeField]
    [Min(1)]
    private int m_count = 2;

    [SerializeField]
    private bool m_powerOfTwo = true;

    [SerializeField]
    private bool m_reverse;

    [SerializeField]
    private bool m_useNegativeValues = false;

    [SerializeField]
    private int m_randomNumberRange = 100;

    [SerializeField]
    [Min(1)]
    private int m_firstNCount = 20;

    private Sorter m_sorter;
    
    private bool QuickTest(int count, bool powerOfTwo, bool reverse, bool useNegatives, int firstN)
    {
        count = powerOfTwo ? Mathf.NextPowerOfTwo(count) : count;
        ComputeBuffer values = new ComputeBuffer(count, sizeof(int));
        values.SetCounterValue(0);

        int[] data = new int[count];
        
        for (int i = 0; i < count; i++)
            data[i] = Random.Range(useNegatives ? -m_randomNumberRange : 0, m_randomNumberRange);

        values.SetData(data);

        using (Sorter sorter = new Sorter(m_mergeSortComputeShader))
        {
            sorter.Sort(values, reverse: false);
        }

        int[] gpuResult = new int[count];
        values.GetData(gpuResult);

        int[] cpuResult;
        
        if (firstN < 0)
        {
            cpuResult = data.OrderBy(v => reverse ? -v : v).ToArray();
        }
        else
        {
            firstN = Mathf.Min(firstN, count);

            int[] firstHalf = new int[firstN];
            int[] secondHalf = new int[count - firstN];

            for (int i = 0; i < firstN; i++)
                firstHalf[i] = data[i];

            for (int i = 0; i < count - firstN; i++)
                secondHalf[i] = data[i + firstN];

            firstHalf = firstHalf.OrderBy(v => reverse ? -v : v).ToArray();

            cpuResult = firstHalf.Concat(secondHalf).ToArray();
        }
        

        bool match = cpuResult.Length == gpuResult.Length;

        if (match)
        {
            int valuesThatMatter = firstN < 0 ? cpuResult.Length : Mathf.Min(firstN, cpuResult.Length);

            for (int i = 0; i < valuesThatMatter; i++)
            {
                if (cpuResult[i] != gpuResult[i])
                {
                    match = false;
                    break;
                }
            }
        }

        if (!match)
            Debug.Log($"GPU: {gpuResult.ToFormattedString()} ({gpuResult.Length}) is not equal to \n CPU: {cpuResult.ToFormattedString()} ({cpuResult.Length})");
        else if (firstN > 0)
        {
            Debug.Log("CPU: " + cpuResult.ToFormattedString() + "\nGPU: " + gpuResult.ToFormattedString());
        }

        values.Dispose();
        values.Release();

        return match;
    }

    public int superTestMin;
    public int superTestMax;
    public int superTestRuns;

    [ContextMenu("Run Test")]
    private void RunTest() => RunTest(m_count, 60);
    
    [ContextMenu("Run SuperTest")]
    private void RunSuperTest()
    {
        for (int i = superTestMin; i < superTestMax; i++)
        {
            Debug.Log("----- " + i + " -----");
            RunTest(i, superTestRuns);
        }
    }

    private void RunTest(int count, int testRuns)
    {
        void TestSettings(bool powerOfTwo, bool reverse, bool useNegatives, int firstN = -1)
        {
            int failed = -1;
            for (int i = 0; i < testRuns; i++)
            {
                if (!QuickTest(count, powerOfTwo, reverse, useNegatives, firstN))
                {
                    failed = i;
                    break;
                }
            }

            if (failed < 0)
            {
                Debug.Log("Success on settings: ".AddColour(Color.green) + $"Power Of Two = {powerOfTwo}, Reverse = {reverse}, Negatives = {useNegatives}, FirstN = {firstN}");
            }
            else
            {
                //if (failed >= 0)
                    Debug.Log($"Failure at attempt {failed} on settings: ".AddColour(Color.red) + $"Power Of Two = {powerOfTwo}, Reverse = {reverse}, Negatives = {useNegatives}, FirstN = {firstN}");
            }
        }

        TestSettings(true, true, true);
        TestSettings(true, true, false);
        TestSettings(true, false, true);
        TestSettings(true, false, false);
        TestSettings(false, true, true);
        TestSettings(false, true, false);
        TestSettings(false, false, true);
        TestSettings(false, false, false);

        TestSettings(true, true, true, m_firstNCount);
        TestSettings(true, true, false, m_firstNCount);
        TestSettings(true, false, true, m_firstNCount);
        TestSettings(true, false, false, m_firstNCount);
        TestSettings(false, true, true, m_firstNCount);
        TestSettings(false, true, false, m_firstNCount);
        TestSettings(false, false, true, m_firstNCount);
        TestSettings(false, false, false, m_firstNCount);
    }

}
