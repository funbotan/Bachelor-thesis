using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSI_GIS
{
    class OuterPoly : Poly
    {
        public InnerPoly[] IP;
        int[,] cutsTable; // Представление графа соединений внутренних полигонов. Структура: 0 обозначает внешний полигон, внутренние нумеруются от 1, ячейка [i,j] указывает, какой вершиной полигон i соединен с j.

        public OuterPoly(string arg) : base(arg) { }

        // Для доступа к полигонам в соответствии с нумерацией cutsTable
        public Poly this[int key]
        {
            get
            {
                if (key == 0) return this;
                else return IP[key - 1];
            }
        }

        // Проверяет, можно ли провести данную линию
        bool lineAllowed(LineD l)
        {
            for (int p = 0; p <= IP.Length; p++)
                if (this[p].getSides().Any(side => l.intersects(side)))
                    return false;
            return true;
        }

        // Находит кратчайшее соединение двух полигонгов; точный алгоритм O(N^2)
        Connection shortestN2(int p0, int p1, bool caution)
        {
            int v0 = 0, v1 = 0;
            double bestDist = double.PositiveInfinity;
            for (int tv0 = 0; tv0 < this[p0].vert.Length; tv0++)
            {
                for (int tv1 = 0; tv1 < this[p1].vert.Length; tv1++)
                {
                    if (this[p0].vert[tv0] % this[p1].vert[tv1] < bestDist &&
                        (!caution || lineAllowed(new LineD(this[p0].vert[tv0], this[p1].vert[tv1]))))
                    {
                        v0 = tv0;
                        v1 = tv1;
                        bestDist = this[p0].vert[tv0] % this[p1].vert[tv1];
                    }
                }
            }
            return new Connection()
            {
                v0 = v0,
                v1 = v1,
                len = bestDist
            };
        }

        // Построение исходного графа соединений
        void buildPath(int p0, int p1, bool caution = false)
        {
            if (p0 == p1) return;
            if (cutsTable[p0, p1] != -1) return;
            if (p1 == 0)
            {
                p1 = p0;
                p0 = 0;
            }
            Connection con = shortestN2(p0, p1, caution);
            if (!lineAllowed(new LineD(this[p0].vert[con.v0], this[p1].vert[con.v1])))
                return;
            cutsTable[p0, p1] = con.v0;
            cutsTable[p1, p0] = con.v1;
        }

        // Алгоритм Прима для cutsTable
        bool Prim()
        {
            int[,] newTable = new int[IP.Length + 1, IP.Length + 1];
            for (int i = 0; i <= IP.Length; i++)
                for (int j = 0; j <= IP.Length; j++)
                    newTable[i, j] = -1;
            bool[] vertSet = new bool[IP.Length + 1];
            vertSet.Initialize();
            vertSet[0] = true;
            List<Connection> options;
            while (vertSet.Any(v => !v))
            {
                options = new List<Connection>();
                for (int i = 0; i <= IP.Length; i++)
                    if (vertSet[i])
                        for (int j = 0; j <= IP.Length; j++)
                            if (!vertSet[j] && cutsTable[i, j] > -1)
                                options.Add(new Connection() {
                                    v0 = i,
                                    v1 = j,
                                    len = this[i].vert[cutsTable[i, j]] % this[j].vert[cutsTable[j, i]]
                                });
                if (options.Count == 0) return false;
                Connection chosen = options.OrderBy(con => con.len).First();
                newTable[chosen.v0, chosen.v1] = cutsTable[chosen.v0, chosen.v1];
                newTable[chosen.v1, chosen.v0] = cutsTable[chosen.v1, chosen.v0];
                vertSet[chosen.v1] = true;
            }
            Array.Copy(newTable, 0, cutsTable, 0, newTable.Length);
            return true;
        }
        
        // Удаление внутренних полигонов
        public void unite()
        {
            int N = IP.Length;
            cutsTable = new int[N + 1, N + 1];
            for (int i = 0; i <= N; i++)
                for (int j = 0; j <= N; j++)
                    cutsTable[i, j] = -1;
            // Строим граф всх возможных соединений между полигонами
            for (int p0 = 0; p0 <= N; p0++)
                for (int p1 = 0; p1 <= N; p1++)
                    buildPath(p0, p1);
            // Оптимизируем его алгоритмом Прима
            bool easymode = Prim();
            if (!easymode)
            {
                for (int p0 = 0; p0 <= N; p0++)
                    for (int p1 = 0; p1 <= N; p1++)
                        buildPath(p0, p1, true);
                Prim();
            }
            // Теперь нужно пройтись по всем вершинам и собрать единый полигон
            List<Vertex> path = new List<Vertex>(); // Недостроенный полигнон
            int nowPoly = 0;
            int nowVert = 0;
            bool wait = false;
            int jumpsDone = 0;
            Stack<Jump> jumps = new Stack<Jump>();
            while (true)
            {
                for (int i = 0; i <= N; i++)
                {
                    if (cutsTable[nowPoly, i] == nowVert)
                    { // переход ко внутреннему полигону "вниз"
                        jumps.Push(new Jump() { fromPoly = nowPoly, toPoly = i, returnTo = nowVert, returnFrom = cutsTable[i, nowPoly] });
                        int tvert = cutsTable[i, nowPoly];
                        cutsTable[nowPoly, i] = -1;
                        cutsTable[i, nowPoly] = -1;
                        nowVert = tvert;
                        nowPoly = i;
                        path.Add(this[nowPoly].vert[nowVert]);
                        wait = true;
                        goto pathFound;
                    }
                }
                if (jumps.Count > 0 && !wait && jumps.Peek().toPoly == nowPoly && jumps.Peek().returnFrom == nowVert)
                { // Возвращение "наверх"
                    Jump returnJump = jumps.Pop();
                    nowPoly = returnJump.fromPoly;
                    nowVert = returnJump.returnTo;
                    path.Add(this[nowPoly].vert[nowVert]);
                    jumpsDone++;
                    wait = true;
                    goto pathFound;
                }
                nowVert = this[nowPoly].next(nowVert);
                path.Add(this[nowPoly].vert[nowVert]);
                wait = false;
            pathFound:
                if (nowPoly == 0 && nowVert == prev(0) && jumpsDone == IP.Length) break;
            }
            path.Add(path.First());
            vert = new Vertex[path.Count];
            path.ToArray().CopyTo(vert, 0);
        }
    }
}
