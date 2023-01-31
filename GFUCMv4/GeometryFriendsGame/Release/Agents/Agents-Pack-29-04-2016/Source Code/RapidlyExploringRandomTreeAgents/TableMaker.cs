using System;
using System.IO;

namespace GeometryFriendsAgents
{
    class TableMaker
    {

        private int _nC; //number of collectibles
        private int _nO; //number of obstacles
        private int _nsP; //number of rectangle platforms
        private int _ncP; //number of circle platforms
        private float[] _oI; //obstacles info
        private float[] _sPI; //rectangle platforms info
        private float[] _cPI; //circle platforms info
        private float[] _colI; //collectibles info
        private int[,] table;

        public int[,] Table
        {
            get { return table; }
            set { table = value; }
        }
        

        public TableMaker(int[] nI, float[] oI, float[] sPI, float[] cPI, float[] colI)
        {
            _nO = nI[0];
            _nsP = nI[1];
            _ncP = nI[2];
            _nC = nI[3];

            Init(oI, sPI, cPI, colI);

        }

        private void Init(float[] oI, float[] sPI, float[] cPI, float[] colI)
        {
            ////// INIT OBSTACLES ARRAY
            if (_nO > 0)
                _oI = new float[_nO * 4];
            else _oI = new float[4];

            int temp = 1;

            if (_nO > 0)
            {
                while (temp <= _nO)
                {
                    _oI[(temp * 4) - 4] = oI[(temp * 4) - 4];
                    _oI[(temp * 4) - 3] = oI[(temp * 4) - 3];
                    _oI[(temp * 4) - 2] = oI[(temp * 4) - 2];
                    _oI[(temp * 4) - 1] = oI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                _oI[0] = oI[0];
                _oI[1] = oI[1];
                _oI[2] = oI[2];
                _oI[3] = oI[3];
            }

            ////// INIT RECTANGLE PLATFORMS ARRAY

            if (_nsP > 0)
                _sPI = new float[_nsP * 4];
            else _sPI = new float[4];

            temp = 1;

            if (_nsP > 0)
            {
                while (temp <= _nsP)
                {
                    _sPI[(temp * 4) - 4] = sPI[(temp * 4) - 4];
                    _sPI[(temp * 4) - 3] = sPI[(temp * 4) - 3];
                    _sPI[(temp * 4) - 2] = sPI[(temp * 4) - 2];
                    _sPI[(temp * 4) - 1] = sPI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                _sPI[0] = sPI[0];
                _sPI[1] = sPI[1];
                _sPI[2] = sPI[2];
                _sPI[3] = sPI[3];
            }

            ////// INIT CIRCLE PLATFORM ARRAY

            if (_ncP > 0)
                _cPI = new float[_ncP * 4];
            else _cPI = new float[4];

            temp = 1;

            if (_ncP > 0)
            {
                while (temp <= _ncP)
                {
                    _cPI[(temp * 4) - 4] = cPI[(temp * 4) - 4];
                    _cPI[(temp * 4) - 3] = cPI[(temp * 4) - 3];
                    _cPI[(temp * 4) - 2] = cPI[(temp * 4) - 2];
                    _cPI[(temp * 4) - 1] = cPI[(temp * 4) - 1];
                    temp++;
                }
            }
            else
            {
                _cPI[0] = cPI[0];
                _cPI[1] = cPI[1];
                _cPI[2] = cPI[2];
                _cPI[3] = cPI[3];
            }

            ////// INIT COLLECTIBLES

            _colI = new float[_nC * 2];

            temp = 1;
            while (temp <= _nC)
            {

                _colI[(temp * 2) - 2] = colI[(temp * 2) - 2];
                _colI[(temp * 2) - 1] = colI[(temp * 2) - 1];

                temp++;
            }
        }

        public void makeTable()
        {
            table = new int[90, 150];

            placeObstacles();
            placeCirclePlatforms();
            placeRectanglePlatforms();
            placeCollectables();
            
        }

        private void placeObstacles()
        {

            float posX;
            float posY;
            float h;
            float w;
            float fstartR;
            float fstartC;
            float fendR;
            float fendC;
            int startR;
            int startC;
            int endR;
            int endC;

            for (int n = 0; n < _nO; n++)
            {
                posX = _oI[(n * 4)];
                posY = _oI[(n * 4) + 1];
                h = _oI[(n * 4) + 2];
                w = _oI[(n * 4) + 3];
                
                fstartC = (posX - (w / 2) -40)/8;
                fstartR = (posY - (h / 2) -40)/8;
                fendC = (posX + (w / 2) -40)/8 - 1;
                fendR = (posY + (h / 2) -40)/8 - 1;

                startC = (int) fstartC;
                startR = (int) fstartR;
                endC = (int) fendC;
                endR = (int) fendR;


                for (int r = startR; r <= endR; r++)
                {
                    for (int c = startC; c <= endC; c++)
                    {
                        table[r, c] = 1;
                    }
                }

            }
        }

        private void placeCirclePlatforms()
        {

            float posX;
            float posY;
            float h;
            float w;
            float fstartR;
            float fstartC;
            float fendR;
            float fendC;
            int startR;
            int startC;
            int endR;
            int endC;

            for (int n = 0; n < _ncP; n++)
            {
                posX = _cPI[(n * 4)];
                posY = _cPI[(n * 4) + 1];
                h = _cPI[(n * 4) + 2];
                w = _cPI[(n * 4) + 3];

                fstartC = (posX - (w / 2) - 40) / 8;
                fstartR = (posY - (h / 2) - 40) / 8;
                fendC = (posX + (w / 2) - 40) / 8 - 1;
                fendR = (posY + (h / 2) - 40) / 8 - 1;

                startC = (int)fstartC;
                startR = (int)fstartR;
                endC = (int)fendC;
                endR = (int)fendR;


                for (int r = startR; r <= endR; r++)
                {
                    for (int c = startC; c <= endC; c++)
                    {
                        table[r, c] = 2;
                    }
                }

            }
        }

        private void placeRectanglePlatforms()
        {

            float posX;
            float posY;
            float h;
            float w;
            float fstartR;
            float fstartC;
            float fendR;
            float fendC;
            int startR;
            int startC;
            int endR;
            int endC;

            for (int n = 0; n < _nsP; n++)
            {
                posX = _sPI[(n * 4)];
                posY = _sPI[(n * 4) + 1];
                h = _sPI[(n * 4) + 2];
                w = _sPI[(n * 4) + 3];

                fstartC = (posX - (w / 2) - 40) / 8;
                fstartR = (posY - (h / 2) - 40) / 8;
                fendC = (posX + (w / 2) - 40) / 8 - 1;
                fendR = (posY + (h / 2) - 40) / 8 - 1;

                startC = (int)fstartC;
                startR = (int)fstartR;
                endC = (int)fendC;
                endR = (int)fendR;


                for (int r = startR; r <= endR; r++)
                {
                    for (int c = startC; c <= endC; c++)
                    {
                        table[r, c] = 3;
                    }
                }

            }
        }

        private void placeCollectables()
        {
            float posX;
            float posY;
            int posXfinal;
            int posYfinal;

            //TODO: add 2 squares on each side

            for (int n = 0; n < _nC; n++)
            {
                posX = _colI[(n * 2)];
                posY = _colI[(n * 2) + 1];

                posX = (posX - 40 - 16) / 8;
                posXfinal = (int)posX;
                posY = (posY - 40 - 16) / 8;
                posYfinal = (int)posY;

                for (int itery = posYfinal; itery < posYfinal + 4; itery++)
                    for (int iterx = posXfinal; iterx < posXfinal + 4; iterx++)
                        table[itery, iterx] = 7;

            }
        }

        public void printTable()
        {
            String line;
			String filePath = Directory.GetCurrentDirectory();
			filePath += "table.txt";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@filePath, true))
            {
                for (int r = 0; r < 90; r++)
                {
                    line = "";
                    for (int c = 0; c < 150; c++)
                    {
                        line += table[r, c];
                    }
                    file.WriteLine(line);
                }
                    
            }
        }
    }
}
