namespace fyiReporting.RDL
{
  using System;
  using System.Drawing;
  using System.Drawing.Drawing2D;
  using System.Drawing.Imaging;
  using System.IO;
  using System.Runtime.InteropServices;

  internal abstract class ChartBase : IDisposable
  {
    public MemoryStream _aStream;
    protected Bitmap _bm;
    protected Chart _ChartDefn;
    private MatrixCellEntry[,] _DataDefn;
    protected int _gridIncrs = 10;
    protected int _LastCategoryWidth = 0;
    protected Metafile _mf;
    protected Row _row;
    private Brush[] _SeriesBrush;
    private ChartMarkerEnum[] _SeriesMarker;
    protected bool _showToolTips;
    protected bool _showToolTipsX;
    protected string _tooltipXFormat;
    protected string _tooltipYFormat;
    protected ChartLayout Layout;

    internal ChartBase(Report r, Row row, Chart c, MatrixCellEntry[,] m, Expression showTooltips, Expression showTooltipsX, Expression _ToolTipYFormat, Expression _ToolTipXFormat)
    {
      this._ChartDefn = c;
      this._row = row;
      this._DataDefn = m;
      this._bm = null;
      int width = this._ChartDefn.WidthCalc(r, null);
      int height = RSize.PixelsFromPoints(this._ChartDefn.HeightOrOwnerHeight);
      this.Layout = new ChartLayout(width, height);
      this._SeriesBrush = null;
      this._SeriesMarker = null;
      this._showToolTips = showTooltips.EvaluateBoolean(r, row);
      this._showToolTipsX = showTooltipsX.EvaluateBoolean(r, row);
      this._tooltipYFormat = _ToolTipYFormat.EvaluateString(r, row);
      this._tooltipXFormat = _ToolTipXFormat.EvaluateString(r, row);
    }

    protected void AdjustMargins(System.Drawing.Rectangle legendRect, Report rpt, Graphics g)
    {
      if (!this.IsLegendInsidePlotArea())
      {
        if (this.IsLegendLeft())
        {
          this.Layout.LeftMargin += legendRect.Width;
        }
        else if (this.IsLegendRight())
        {
          this.Layout.RightMargin += legendRect.Width;
        }
        if (this.IsLegendTop())
        {
          this.Layout.TopMargin += legendRect.Height;
        }
        else if (this.IsLegendBottom())
        {
          this.Layout.BottomMargin += legendRect.Height;
        }
      }
      int pixelsX = new RSize(this.ChartDefn.OwnerReport, ".2 in").PixelsX;
      if (this.Layout.RightMargin < (pixelsX + (this._LastCategoryWidth / 2)))
      {
        this.Layout.RightMargin = pixelsX + (this._LastCategoryWidth / 2);
      }
      if (this.Layout.LeftMargin < pixelsX)
      {
        this.Layout.LeftMargin = pixelsX;
      }
      if (this.Layout.TopMargin < pixelsX)
      {
        this.Layout.TopMargin = pixelsX;
      }
      if (this.Layout.BottomMargin < pixelsX)
      {
        this.Layout.BottomMargin = pixelsX;
      }
    }

    protected Bitmap CreateSizedBitmap()
    {
      if (this._bm != null)
      {
        this._bm.Dispose();
        this._bm = null;
      }
      this._bm = new Bitmap(this.Layout.Width, this.Layout.Height);
      return this._bm;
    }

    protected Bitmap CreateSizedBitmap(int W, int H)
    {
      if (this._bm != null)
      {
        this._bm.Dispose();
        this._bm = null;
      }
      this._bm = new Bitmap(W, H);
      return this._bm;
    }

    public void Dispose()
    {
      if (this._bm != null)
      {
        this._bm.Dispose();
      }
    }

    internal virtual void Draw(Report rpt)
    {
    }

    protected void DrawCategoryLabel(Report rpt, Graphics g, string t, Style a, System.Drawing.Rectangle rect)
    {
      if (t != null)
      {
        Row r = this.FirstChartRow(rpt);
        if (a != null)
        {
          a.DrawString(rpt, g, t, t.GetTypeCode(), r, rect);
          a.DrawBorder(rpt, g, r, rect);
        }
        else
        {
          Style.DrawStringDefaults(g, t, rect);
        }
      }
    }

    protected Size DrawCategoryTitleMeasure(Report rpt, Graphics g, string t, Style a)
    {
      Size empty = Size.Empty;
      Row r = this.FirstChartRow(rpt);
      if ((t != null) && (t != ""))
      {
        if (a != null)
        {
          empty = a.MeasureString(rpt, g, t, t.GetTypeCode(), r, 0x7fffffff);
        }
        else
        {
          empty = Style.MeasureStringDefaults(rpt, g, t, t.GetTypeCode(), r, 0x7fffffff);
        }
      }
      return empty;
    }

    protected void DrawChartStyle(Report rpt, Graphics g)
    {
      System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.Layout.Width, this.Layout.Height);
      if (this._ChartDefn.Style == null)
      {
        g.FillRectangle(Brushes.White, rect);
      }
      else
      {
        Row r = this.FirstChartRow(rpt);
        this._ChartDefn.Style.DrawBorder(rpt, g, r, rect);
        this._ChartDefn.Style.DrawBackground(rpt, g, r, rect);
      }
    }

    protected void DrawDataPoint(Report rpt, Graphics g, Point p, int row, int col)
    {
      this.DrawDataPoint(rpt, g, p, System.Drawing.Rectangle.Empty, row, col);
    }

    protected void DrawDataPoint(Report rpt, Graphics g, System.Drawing.Rectangle rect, int row, int col)
    {
      this.DrawDataPoint(rpt, g, Point.Empty, rect, row, col);
    }

    private void DrawDataPoint(Report rpt, Graphics g, Point p, System.Drawing.Rectangle rect, int row, int col)
    {
      MatrixCellEntry entry = this._DataDefn[row, col];
      if (entry != null)
      {
        ChartExpression displayItem = (ChartExpression)entry.DisplayItem;
        DataPoint dP = displayItem.DP;
        if ((dP.DataLabel != null) && dP.DataLabel.Visible)
        {
          Row row2;
          TypeCode typeCode;
          Size size;
          this._ChartDefn.ChartMatrix.SetMyData(rpt, entry.Data);
          if (entry.Data.Data.Count > 0)
          {
            row2 = entry.Data.Data[0];
          }
          else
          {
            row2 = null;
          }
          object o = null;
          if (dP.DataLabel.Value == null)
          {
            o = displayItem.Value.EvaluateDouble(rpt, row2);
            typeCode = TypeCode.Double;
          }
          else
          {
            o = dP.DataLabel.Value.Evaluate(rpt, row2);
            typeCode = dP.DataLabel.Value.GetTypeCode();
          }
          if (dP.DataLabel.Style == null)
          {
            if (rect == System.Drawing.Rectangle.Empty)
            {
              size = Style.MeasureStringDefaults(rpt, g, o, typeCode, row2, 0x7fffffff);
              rect = new System.Drawing.Rectangle(p, size);
            }
            Style.DrawStringDefaults(g, o, rect);
          }
          else
          {
            if (rect == System.Drawing.Rectangle.Empty)
            {
              size = dP.DataLabel.Style.MeasureString(rpt, g, o, typeCode, row2, 0x7fffffff);
              rect = new System.Drawing.Rectangle(p, size);
            }
            dP.DataLabel.Style.DrawString(rpt, g, o, typeCode, row2, rect);
          }
        }
      }
    }

    protected System.Drawing.Rectangle DrawLegend(Report rpt, Graphics g, bool bMarker, bool bBeforePlotDrawn)
    {
      System.Drawing.Rectangle rectangle;
      Legend legend = this._ChartDefn.Legend;
      if (legend == null)
      {
        return System.Drawing.Rectangle.Empty;
      }
      if (!legend.Visible)
      {
        return System.Drawing.Rectangle.Empty;
      }
      if (this._ChartDefn.SeriesGroupings == null)
      {
        return System.Drawing.Rectangle.Empty;
      }
      if (bBeforePlotDrawn)
      {
        if (this.IsLegendInsidePlotArea())
        {
          return System.Drawing.Rectangle.Empty;
        }
      }
      else if (!this.IsLegendInsidePlotArea())
      {
        return System.Drawing.Rectangle.Empty;
      }
      Font f = null;
      Brush brush = null;
      StringFormat sf = null;
      Style style = legend.Style;
      try
      {
        int leftMargin;
        int num2;
        int num3;
        int num4;
        int num5;
        int num8;
        int num9;
        int num10;
        if (style == null)
        {
          f = new Font("Arial", 10f);
          brush = new SolidBrush(Color.Black);
          sf = new StringFormat();
          sf.Alignment = StringAlignment.Near;
        }
        else
        {
          f = style.GetFont(rpt, null);
          brush = style.GetBrush(rpt, null);
          sf = style.GetStringFormat(rpt, null, StringAlignment.Near);
        }
        sf.FormatFlags |= StringFormatFlags.NoWrap;
        Size[] sizeArray = this.DrawLegendMeasure(rpt, g, f, sf, new SizeF((float)this.Layout.Width, (float)this.Layout.Height), out num4, out num5);
        int boxSize = (int)(num5 * 0.8);
        int num7 = 0;
        switch (legend.Layout)
        {
          case LegendLayoutEnum.Row:
            num8 = 0;
            num10 = 0;
            goto Label_01C0;

          case LegendLayoutEnum.Table:
            num8 = num7 = (num4 + (boxSize * 2)) * 2;
            num3 = num5 + ((int)(num5 * 0.1));
            num9 = (num3 * (this.SeriesCount + (this.SeriesCount % 2))) / 2;
            goto Label_0262;

          default:
            num8 = num7 = num4 + (boxSize * 2);
            num3 = num5 + ((int)(num5 * 0.1));
            num9 = num3 * this.SeriesCount;
            goto Label_0262;
        }
      Label_0189:
        if (sizeArray[num10].Width != 0)
        {
          num8 += sizeArray[num10].Width + (boxSize * 2);
        }
        num10++;
      Label_01C0:
        if (num10 < this.SeriesCount)
        {
          goto Label_0189;
        }
        num9 = num5 + ((int)(num5 * 0.1));
        num3 = num9;
        num7 = num4 + (boxSize * 2);
        sf.Alignment = StringAlignment.Near;
      Label_0262:
        if (this.IsLegendInsidePlotArea())
        {
          switch (legend.Position)
          {
            case LegendPositionEnum.TopLeft:
            case LegendPositionEnum.LeftTop:
              leftMargin = this.Layout.PlotArea.X + 2;
              num2 = this.Layout.PlotArea.Y + 2;
              goto Label_06C3;

            case LegendPositionEnum.TopCenter:
              leftMargin = (this.Layout.PlotArea.X + (this.Layout.PlotArea.Width / 2)) - (num8 / 2);
              num2 = this.Layout.PlotArea.Y + 2;
              goto Label_06C3;

            case LegendPositionEnum.LeftCenter:
              leftMargin = this.Layout.PlotArea.X + 2;
              num2 = (this.Layout.PlotArea.Y + (this.Layout.PlotArea.Height / 2)) - (num9 / 2);
              goto Label_06C3;

            case LegendPositionEnum.LeftBottom:
            case LegendPositionEnum.BottomLeft:
              leftMargin = this.Layout.PlotArea.X + 2;
              num2 = ((this.Layout.PlotArea.Y + this.Layout.PlotArea.Height) - num9) - 2;
              goto Label_06C3;

            case LegendPositionEnum.RightCenter:
              leftMargin = ((this.Layout.PlotArea.X + this.Layout.PlotArea.Width) - num8) - 2;
              num2 = (this.Layout.PlotArea.Y + (this.Layout.PlotArea.Height / 2)) - (num9 / 2);
              goto Label_06C3;

            case LegendPositionEnum.RightBottom:
            case LegendPositionEnum.BottomRight:
              leftMargin = (this.Layout.PlotArea.X + this.Layout.PlotArea.Width) - num8;
              num2 = ((this.Layout.PlotArea.Y + this.Layout.PlotArea.Height) - num9) - 2;
              goto Label_06C3;

            case LegendPositionEnum.BottomCenter:
              leftMargin = (this.Layout.PlotArea.X + (this.Layout.PlotArea.Width / 2)) - (num8 / 2);
              num2 = ((this.Layout.PlotArea.Y + this.Layout.PlotArea.Height) - num9) - 2;
              goto Label_06C3;
          }
          leftMargin = ((this.Layout.PlotArea.X + this.Layout.PlotArea.Width) - num8) - 2;
          num2 = this.Layout.PlotArea.Y + 2;
          goto Label_06C3;
        }
        switch (legend.Position)
        {
          case LegendPositionEnum.TopLeft:
          case LegendPositionEnum.LeftTop:
            leftMargin = 2;
            num2 = this.Layout.TopMargin + 2;
            goto Label_06C3;

          case LegendPositionEnum.TopCenter:
            leftMargin = (this.Layout.Width / 2) - (num8 / 2);
            num2 = this.Layout.TopMargin + 2;
            goto Label_06C3;

          case LegendPositionEnum.LeftCenter:
            leftMargin = 2;
            num2 = (this.Layout.Height / 2) - (num9 / 2);
            goto Label_06C3;

          case LegendPositionEnum.LeftBottom:
          case LegendPositionEnum.BottomLeft:
            if (!this.IsLegendInsidePlotArea())
            {
              break;
            }
            leftMargin = this.Layout.LeftMargin;
            goto Label_05E5;

          case LegendPositionEnum.RightCenter:
            leftMargin = (this.Layout.Width - num8) - 2;
            num2 = (this.Layout.Height / 2) - (num9 / 2);
            goto Label_06C3;

          case LegendPositionEnum.RightBottom:
          case LegendPositionEnum.BottomRight:
            leftMargin = this.Layout.Width - num8;
            num2 = (this.Layout.Height - num9) - 2;
            goto Label_06C3;

          case LegendPositionEnum.BottomCenter:
            leftMargin = (this.Layout.Width / 2) - (num8 / 2);
            num2 = (this.Layout.Height - num9) - 2;
            goto Label_06C3;

          default:
            leftMargin = (this.Layout.Width - num8) - 2;
            num2 = this.Layout.TopMargin + 2;
            goto Label_06C3;
        }
        leftMargin = 0;
      Label_05E5:
        num2 = (this.Layout.Height - num9) - 2;
      Label_06C3:
        rectangle = new System.Drawing.Rectangle(leftMargin - 1, num2 - 1, num8 + 2, num9 + 2);
        if (style != null)
        {
          style.DrawBackground(rpt, g, null, rectangle);
          style.DrawBorder(rpt, g, null, rectangle);
        }
        int num11 = leftMargin;
        ChartMarkerEnum marker = (this.ChartDefn.Type == ChartTypeEnum.Bubble) ? ChartMarkerEnum.Bubble : ChartMarkerEnum.None;
        for (int i = 1; i <= this.SeriesCount; i++)
        {
          System.Drawing.Rectangle rectangle2;
          SolidBrush brush2;
          HatchBrush brush3;
          string seriesValue = this.GetSeriesValue(rpt, i);
          if (!(seriesValue != ""))
          {
            continue;
          }
          Type type = null;
          type = this.GetSeriesBrush(rpt, 1, i).GetType();
          marker = ChartMarkerEnum.None;
          bool flag = this.GetPlotType(rpt, i, 1).ToUpper() == "LINE";
          if ((bMarker || flag) || ((this.ChartDefn.Type == ChartTypeEnum.Scatter) && (type == typeof(HatchBrush))))
          {
            marker = this.SeriesMarker[i - 1];
          }
          if (flag && (this.ChartDefn.Type == ChartTypeEnum.Scatter))
          {
            marker = ChartMarkerEnum.Line;
          }
          if (this.getNoMarkerVal(rpt, i, 1))
          {
            marker = ChartMarkerEnum.Line;
          }
          string str2 = this.getLineSize(rpt, i, 1);
          int intLineSize = 2;
          string str3 = str2;
          if (str3 != null)
          {
            if (!(str3 == "Small"))
            {
              if (str3 == "Regular")
              {
                goto Label_085C;
              }
              if (str3 == "Large")
              {
                goto Label_0861;
              }
              if (str3 == "Extra Large")
              {
                goto Label_0866;
              }
              if (str3 == "Super Size")
              {
                goto Label_086B;
              }
            }
            else
            {
              intLineSize = 1;
            }
          }
          goto Label_0870;
        Label_085C:
          intLineSize = 2;
          goto Label_0870;
        Label_0861:
          intLineSize = 3;
          goto Label_0870;
        Label_0866:
          intLineSize = 4;
          goto Label_0870;
        Label_086B:
          intLineSize = 5;
        Label_0870:
          switch (legend.Layout)
          {
            case LegendLayoutEnum.Row:
              rectangle2 = new System.Drawing.Rectangle((leftMargin + boxSize) + (boxSize / 2), num2, (num7 - boxSize) - (boxSize / 2), num3);
              if (!(seriesValue != ""))
              {
                continue;
              }
              g.DrawString(seriesValue, f, brush, rectangle2, sf);
              if (((marker == ChartMarkerEnum.None) && (this.ChartDefn.Type != ChartTypeEnum.Scatter)) || (type != typeof(HatchBrush)))
              {
                break;
              }
              brush3 = (HatchBrush)this.GetSeriesBrush(rpt, 1, i);
              brush2 = new SolidBrush(brush3.ForegroundColor);
              this.DrawLegendBox(g, brush2, marker, leftMargin, num2 + 1, boxSize, intLineSize);
              goto Label_0962;

            case LegendLayoutEnum.Table:
              rectangle2 = new System.Drawing.Rectangle((leftMargin + boxSize) + (boxSize / 2), num2, num4, num3);
              g.DrawString(seriesValue, f, brush, rectangle2, sf);
              if ((marker == ChartMarkerEnum.None) || (type != typeof(HatchBrush)))
              {
                goto Label_0A05;
              }
              brush3 = (HatchBrush)this.GetSeriesBrush(rpt, 1, i);
              brush2 = new SolidBrush(brush3.ForegroundColor);
              this.DrawLegendBox(g, brush2, marker, leftMargin, num2 + 1, boxSize, intLineSize);
              goto Label_0A25;

            default:
              goto Label_0A59;
          }
          this.DrawLegendBox(g, this.GetSeriesBrush(rpt, 1, i), marker, leftMargin, num2 + 1, boxSize, intLineSize);
        Label_0962:
          leftMargin += sizeArray[i - 1].Width + (boxSize * 2);
          continue;
        Label_0A05:
          this.DrawLegendBox(g, this.GetSeriesBrush(rpt, 1, i), marker, leftMargin + 1, num2, boxSize, intLineSize);
        Label_0A25:
          if ((i % 2) == 0)
          {
            num2 += num3;
            leftMargin = num11;
          }
          else
          {
            leftMargin = num11 + (rectangle.Width / 2);
          }
          continue;
        Label_0A59:
          rectangle2 = new System.Drawing.Rectangle((leftMargin + boxSize) + (boxSize / 2), num2, num4, num3);
          g.DrawString(seriesValue, f, brush, rectangle2, sf);
          if ((marker != ChartMarkerEnum.None) && (type == typeof(HatchBrush)))
          {
            brush3 = (HatchBrush)this.GetSeriesBrush(rpt, 1, i);
            brush2 = new SolidBrush(brush3.ForegroundColor);
            this.DrawLegendBox(g, brush2, marker, leftMargin, num2 + 1, boxSize, intLineSize);
          }
          else
          {
            this.DrawLegendBox(g, this.GetSeriesBrush(rpt, 1, i), marker, leftMargin + 1, num2, boxSize, intLineSize);
          }
          num2 += num3;
        }
      }
      finally
      {
        if (f != null)
        {
          f.Dispose();
        }
        if (brush != null)
        {
          brush.Dispose();
        }
        if (sf != null)
        {
          sf.Dispose();
        }
      }
      if (style != null)
      {
        rectangle = style.PaddingAdjust(rpt, null, rectangle, true);
      }
      return rectangle;
    }

    private void DrawLegendBox(Graphics g, Brush b, ChartMarkerEnum marker, int x, int y, int boxSize)
    {
      this.DrawLegendBox(g, b, marker, x, y, boxSize, 2);
    }

    private void DrawLegendBox(Graphics g, Brush b, ChartMarkerEnum marker, int x, int y, int boxSize, int intLineSize)
    {
      Pen pen = null;
      int mSize = boxSize / 2;
      try
      {
        if (marker < ChartMarkerEnum.Count)
        {
          pen = new Pen(b, (float)intLineSize);
          if (this.ChartDefn.Type != ChartTypeEnum.Scatter)
          {
            g.DrawLine(pen, new Point(x, y + ((boxSize + 1) / 2)), new Point(x + boxSize, y + ((boxSize + 1) / 2)));
          }
          x += (boxSize - mSize) / 2;
          y += (boxSize - mSize) / 2;
          if ((mSize % 2) == 0)
          {
            mSize++;
          }
        }
        if (marker == ChartMarkerEnum.None)
        {
          g.FillRectangle(b, x, y, boxSize, boxSize);
        }
        else if (marker == ChartMarkerEnum.Bubble)
        {
          g.FillEllipse(b, x, y, boxSize, boxSize);
        }
        else if (marker == ChartMarkerEnum.Line)
        {
          pen = new Pen(b, (float)intLineSize);
          g.DrawLine(pen, new Point(x, y + ((boxSize + 1) / 2)), new Point(x + boxSize, y + ((boxSize + 1) / 2)));
        }
        else
        {
          this.DrawLegendMarker(g, b, pen, marker, x, y, mSize);
        }
      }
      finally
      {
        if (pen != null)
        {
          pen.Dispose();
        }
      }
    }

    internal void DrawLegendMarker(Graphics g, Brush b, Pen p, ChartMarkerEnum marker, int x, int y, int mSize)
    {
      PointF[] tfArray;
      switch (marker)
      {
        case ChartMarkerEnum.Circle:
        case ChartMarkerEnum.Bubble:
          g.FillEllipse(b, x, y, mSize, mSize);
          break;

        case ChartMarkerEnum.Square:
          g.FillRectangle(b, x, y, mSize, mSize);
          break;

        case ChartMarkerEnum.Triangle:
          tfArray = new PointF[4];
          tfArray[0] = tfArray[3] = new PointF((float)(x + ((mSize + 1) / 2)), (float)y);
          tfArray[1] = new PointF((float)x, (float)(y + mSize));
          tfArray[2] = new PointF((float)(x + mSize), (float)(y + mSize));
          g.FillPolygon(b, tfArray);
          break;

        case ChartMarkerEnum.Plus:
          p = new Pen(p.Brush, 2f);
          g.DrawLine(p, new Point(x + ((mSize + 1) / 2), y), new Point(x + ((mSize + 1) / 2), y + mSize));
          break;

        case ChartMarkerEnum.X:
          p = new Pen(p.Brush, 2f);
          g.DrawLine(p, new Point(x, y), new Point(x + mSize, y + mSize));
          g.DrawLine(p, new Point(x, y + mSize), new Point(x + mSize, y));
          break;

        case ChartMarkerEnum.Diamond:
          tfArray = new PointF[5];
          tfArray[0] = tfArray[4] = (PointF)new Point(x + ((mSize + 1) / 2), y);
          tfArray[1] = new PointF((float)x, (float)(y + ((mSize + 1) / 2)));
          tfArray[2] = new PointF((float)(x + ((mSize + 1) / 2)), (float)(y + mSize));
          tfArray[3] = new PointF((float)(x + mSize), (float)(y + ((mSize + 1) / 2)));
          g.FillPolygon(b, tfArray);
          break;
      }
    }

    protected Size[] DrawLegendMeasure(Report rpt, Graphics g, Font f, StringFormat sf, SizeF maxSize, out int maxWidth, out int maxHeight)
    {
      Size[] sizeArray = new Size[this.SeriesCount];
      maxWidth = maxHeight = 0;
      for (int i = 1; i <= this.SeriesCount; i++)
      {
        string seriesValue = this.GetSeriesValue(rpt, i);
        if (seriesValue != "")
        {
          SizeF ef = g.MeasureString(seriesValue, f, maxSize, sf);
          sizeArray[i - 1] = new Size((int)Math.Ceiling((double)ef.Width), (int)Math.Ceiling((double)ef.Height));
          if (sizeArray[i - 1].Width > maxWidth)
          {
            maxWidth = sizeArray[i - 1].Width;
          }
          if (sizeArray[i - 1].Height > maxHeight)
          {
            maxHeight = sizeArray[i - 1].Height;
          }
        }
      }
      return sizeArray;
    }

    protected void DrawPlotAreaStyle(Report rpt, Graphics g, System.Drawing.Rectangle crect)
    {
      if ((this._ChartDefn.PlotArea != null) && (this._ChartDefn.PlotArea.Style != null))
      {
        System.Drawing.Rectangle plotArea = this.Layout.PlotArea;
        Style style = this._ChartDefn.PlotArea.Style;
        Row r = this.FirstChartRow(rpt);
        if (plotArea.IntersectsWith(crect))
        {
          using (null)
          {
            style.DrawBackground(rpt, g, r, plotArea);
          }
        }
        else
        {
          style.DrawBackground(rpt, g, r, plotArea);
        }
      }
    }

    protected void DrawTitle(Report rpt, Graphics g, Title t, System.Drawing.Rectangle rect)
    {
      if ((t != null) && (t.Caption != null))
      {
        Row row = this.FirstChartRow(rpt);
        object o = t.Caption.Evaluate(rpt, row);
        if (t.Style != null)
        {
          t.Style.DrawString(rpt, g, o, t.Caption.GetTypeCode(), row, rect);
          t.Style.DrawBorder(rpt, g, row, rect);
        }
        else
        {
          Style.DrawStringDefaults(g, o, rect);
        }
      }
    }

    protected Size DrawTitleMeasure(Report rpt, Graphics g, Title t)
    {
      Size empty = Size.Empty;
      if ((t != null) && (t.Caption != null))
      {
        Row row = this.FirstChartRow(rpt);
        object o = t.Caption.Evaluate(rpt, row);
        if (t.Style != null)
        {
          empty = t.Style.MeasureString(rpt, g, o, t.Caption.GetTypeCode(), row, 0x7fffffff);
        }
        else
        {
          empty = Style.MeasureStringDefaults(rpt, g, o, t.Caption.GetTypeCode(), row, 0x7fffffff);
        }
      }
      return empty;
    }

    private Row FirstChartRow(Report rpt)
    {
      Rows myData = this._ChartDefn.ChartMatrix.GetMyData(rpt);
      if ((myData != null) && (myData.Data.Count > 0))
      {
        return myData.Data[0];
      }
      return null;
    }

    protected object GetCategoryValue(Report rpt, int row, out TypeCode tc)
    {
      Row row2;
      MatrixCellEntry entry = this._DataDefn[row, 0];
      if (entry == null)
      {
        tc = TypeCode.String;
        return "";
      }
      this._ChartDefn.ChartMatrix.SetMyData(rpt, entry.Data);
      if (entry.Data.Data.Count > 0)
      {
        row2 = entry.Data.Data[0];
      }
      else
      {
        row2 = null;
      }
      ChartExpression displayItem = (ChartExpression)entry.DisplayItem;
      object obj2 = displayItem.Value.Evaluate(rpt, row2);
      tc = displayItem.Value.GetTypeCode();
      return obj2;
    }

    protected string getColour(Report rpt, int row, int col)
    {
      string source = null;
      try
      {
        source = ((ChartExpression)this._DataDefn[row, col].DisplayItem).Colour.Source;
      }
      catch
      {
      }
      return source;
    }

    protected double GetDataValue(Report rpt, int row, int col)
    {
      return this.GetDataValue(rpt, row, col, 0);
    }

    protected double GetDataValue(Report rpt, int row, int col, int xyb)
    {
      Row row2;
      MatrixCellEntry entry = this._DataDefn[row, col];
      if (entry == null)
      {
        return 0.0;
      }
      if ((entry.Value != double.MinValue) && (xyb == 0))
      {
        return entry.Value;
      }
      this._ChartDefn.ChartMatrix.SetMyData(rpt, entry.Data);
      if (entry.Data.Data.Count > 0)
      {
        row2 = entry.Data.Data[0];
      }
      else
      {
        row2 = null;
      }
      ChartExpression displayItem = (ChartExpression)entry.DisplayItem;
      double minValue = double.MinValue;
      if (xyb == 0)
      {
        minValue = displayItem.Value.EvaluateDouble(rpt, row2);
        entry.Value = minValue;
      }
      else if (xyb == 1)
      {
        minValue = displayItem.Value2.EvaluateDouble(rpt, row2);
      }
      else if (xyb == 2)
      {
        minValue = displayItem.Value3.EvaluateDouble(rpt, row2);
      }
      return minValue;
    }

    protected string GetDataValueString(Report rpt, int row, int col)
    {
      Row row2;
      MatrixCellEntry entry = this._DataDefn[row, col];
      if (entry == null)
      {
        return null;
      }
      this._ChartDefn.ChartMatrix.SetMyData(rpt, entry.Data);
      if (entry.Data.Data.Count > 0)
      {
        row2 = entry.Data.Data[0];
      }
      else
      {
        row2 = null;
      }
      ChartExpression displayItem = (ChartExpression)entry.DisplayItem;
      return displayItem.Value.EvaluateString(rpt, row2);
    }

    protected string getLineSize(Report rpt, int row, int col)
    {
      string source = null;
      try
      {
        source = ((ChartExpression)this._DataDefn[col, row].DisplayItem).LineSize.Source;
      }
      catch
      {
      }
      return source;
    }

    protected void GetMaxMinDataValue(Report rpt, out double max, out double min, int xyb, int WhichYAxis)
    {
      if (((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), this._ChartDefn.Subtype.EvaluateString(rpt, this._row))) == ChartSubTypeEnum.Stacked)
      {
        this.GetMaxMinDataValueStacked(rpt, out max, out min);
      }
      else
      {
        min = double.MaxValue;
        max = double.MinValue;
        double num = 0.0;
        for (int i = 1; i <= this.CategoryCount; i++)
        {
          for (int j = 1; j <= this.SeriesCount; j++)
          {
            if (WhichYAxis == 2)
            {
              if (this.GetYAxis(rpt, j, 1).ToUpper() == "RIGHT")
              {
                num = this.GetDataValue(rpt, i, j, xyb);
                if (num < min)
                {
                  min = num;
                }
                if (num > max)
                {
                  max = num;
                }
              }
            }
            else if (this.GetYAxis(rpt, j, 1).ToUpper() != "RIGHT")
            {
              num = this.GetDataValue(rpt, i, j, xyb);
              if (num < min)
              {
                min = num;
              }
              if (num > max)
              {
                max = num;
              }
            }
          }
        }
      }
    }

    private void GetMaxMinDataValueStacked(Report rpt, out double max, out double min)
    {
      min = double.MaxValue;
      max = double.MinValue;
      for (int i = 1; i <= this.CategoryCount; i++)
      {
        double num = 0.0;
        for (int j = 1; j <= this.SeriesCount; j++)
        {
          num += this.GetDataValue(rpt, i, j);
        }
        if (num < min)
        {
          min = num;
        }
        if (num > max)
        {
          max = num;
        }
      }
    }

    protected bool getNoMarkerVal(Report rpt, int row, int col)
    {
      bool flag = false;
      try
      {
        flag = bool.Parse(((ChartExpression)this._DataDefn[col, row].DisplayItem).NoMarker.Source);
      }
      catch
      {
      }
      return flag;
    }

    protected string GetPlotType(Report rpt, int row, int col)
    {
      try
      {
        if ((this is ChartColumn) || (this is ChartBubble))
        {
          return ((ChartExpression)this._DataDefn[col, row].DisplayItem).PlotType.Source;
        }
      }
      catch
      {
      }
      return "Auto";
    }

    protected Brush GetSeriesBrush(Report rpt, int row, int col)
    {
      Brush brush = this.SeriesBrush(rpt, this._row, this.ChartDefn.OwnerReport)[col - 1];
      MatrixCellEntry entry = this._DataDefn[row, col];
      if (entry != null)
      {
        ChartExpression displayItem = (ChartExpression)entry.DisplayItem;
        if (((displayItem.DP == null) || (displayItem.DP.Style == null)) || (displayItem.DP.Style.BackgroundColor == null))
        {
          return brush;
        }
        this._ChartDefn.ChartMatrix.SetMyData(rpt, entry.Data);
        Row row2 = (entry.Data.Data.Count > 0) ? entry.Data.Data[0] : null;
        Color color = XmlUtil.ColorFromHtml(displayItem.DP.Style.BackgroundColor.EvaluateString(rpt, row2), Color.Empty, rpt);
        if (color != Color.Empty)
        {
          brush = new SolidBrush(color);
        }
      }
      return brush;
    }

    private Brush GetSeriesBrushEnterpriseTester(int i)
    {
      int alpha = 0x80;
      switch ((i % 10))
      {
        case 1:
          return new SolidBrush(Color.FromArgb(alpha, 0, 0xb0, 240));

        case 2:
          return new SolidBrush(Color.FromArgb(alpha, 0xff, 0x99, 0x33));

        case 3:
          return new SolidBrush(Color.FromArgb(alpha, 0xff, 0, 0));

        case 4:
          return new SolidBrush(Color.FromArgb(alpha, 0x9a, 0xef, 0xf8));

        case 5:
          return new SolidBrush(Color.FromArgb(alpha, 0, 0x70, 0xc0));

        case 6:
          return new SolidBrush(Color.FromArgb(alpha, 0, 0xb0, 80));

        case 7:
          return new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));

        case 8:
          return new SolidBrush(Color.FromArgb(alpha, 0xa3, 0x1d, 0x20));

        case 9:
          return new SolidBrush(Color.FromArgb(alpha, 0x70, 0x30, 160));
      }
      return new SolidBrush(Color.FromArgb(alpha, 0x51, 0xd3, 0x51));
    }

    protected Brush[] GetSeriesBrushes(Report rpt, Row row, ReportDefn defn)
    {
      Brush[] brushArray = new Brush[this.SeriesCount];
      for (int i = 0; i < this.SeriesCount; i++)
      {
        switch (ChartPalette.GetStyle(this.ChartDefn.Palette.EvaluateString(rpt, row), defn.rl))
        {
          case ChartPaletteEnum.Default:
            brushArray[i] = this.GetSeriesBrushesExcel(i);
            break;

          case ChartPaletteEnum.EarthTones:
            brushArray[i] = this.GetSeriesBrushesEarthTones(i);
            break;

          case ChartPaletteEnum.Excel:
            brushArray[i] = this.GetSeriesBrushesExcel(i);
            break;

          case ChartPaletteEnum.GrayScale:
            brushArray[i] = this.GetSeriesBrushesGrayScale(i);
            break;

          case ChartPaletteEnum.Light:
            brushArray[i] = this.GetSeriesBrushesLight(i);
            break;

          case ChartPaletteEnum.Pastel:
            brushArray[i] = this.GetSeriesBrushesPastel(i);
            break;

          case ChartPaletteEnum.SemiTransparent:
            brushArray[i] = this.GetSeriesBrushEnterpriseTester(i);
            break;

          case ChartPaletteEnum.Patterned:
            brushArray[i] = this.GetSeriesBrushesPatterned(i);
            break;

          case ChartPaletteEnum.PatternedBlack:
            brushArray[i] = this.GetSeriesBrushesPatternedBlack(i);
            break;

          case ChartPaletteEnum.Custom:
            brushArray[i] = new SolidBrush(Color.FromName(this.getColour(rpt, 1, i + 1)));
            break;

          default:
            brushArray[i] = this.GetSeriesBrushesExcel(i);
            break;
        }
      }
      return brushArray;
    }

    private Brush GetSeriesBrushesEarthTones(int i)
    {
      switch ((i % 0x16))
      {
        case 0:
          return Brushes.Maroon;

        case 1:
          return Brushes.Brown;

        case 2:
          return Brushes.Chocolate;

        case 3:
          return Brushes.IndianRed;

        case 4:
          return Brushes.Peru;

        case 5:
          return Brushes.BurlyWood;

        case 6:
          return Brushes.AntiqueWhite;

        case 7:
          return Brushes.FloralWhite;

        case 8:
          return Brushes.Ivory;

        case 9:
          return Brushes.LightCoral;

        case 10:
          return Brushes.DarkSalmon;

        case 11:
          return Brushes.LightSalmon;

        case 12:
          return Brushes.PeachPuff;

        case 13:
          return Brushes.NavajoWhite;

        case 14:
          return Brushes.Moccasin;

        case 15:
          return Brushes.PapayaWhip;

        case 0x10:
          return Brushes.Goldenrod;

        case 0x11:
          return Brushes.DarkGoldenrod;

        case 0x12:
          return Brushes.DarkKhaki;

        case 0x13:
          return Brushes.Khaki;

        case 20:
          return Brushes.Beige;

        case 0x15:
          return Brushes.Cornsilk;
      }
      return Brushes.Brown;
    }

    private Brush GetSeriesBrushesExcel(int i)
    {
      switch ((i % 11))
      {
        case 0:
          return Brushes.Blue;

        case 1:
          return Brushes.Red;

        case 2:
          return Brushes.Green;

        case 3:
          return Brushes.Purple;

        case 4:
          return Brushes.DeepSkyBlue;

        case 5:
          return Brushes.Orange;

        case 6:
          return Brushes.Magenta;

        case 7:
          return Brushes.Gold;

        case 8:
          return Brushes.Lime;

        case 9:
          return Brushes.Teal;

        case 10:
          return Brushes.Pink;
      }
      return Brushes.Blue;
    }

    private Brush GetSeriesBrushesGrayScale(int i)
    {
      switch ((i % 10))
      {
        case 0:
          return Brushes.Gray;

        case 1:
          return Brushes.SlateGray;

        case 2:
          return Brushes.DarkGray;

        case 3:
          return Brushes.LightGray;

        case 4:
          return Brushes.DarkSlateGray;

        case 5:
          return Brushes.DimGray;

        case 6:
          return Brushes.LightSlateGray;

        case 7:
          return Brushes.Black;

        case 8:
          return Brushes.White;

        case 9:
          return Brushes.Gainsboro;
      }
      return Brushes.Gray;
    }

    private Brush GetSeriesBrushesLight(int i)
    {
      switch ((i % 13))
      {
        case 0:
          return Brushes.LightBlue;

        case 1:
          return Brushes.LightCoral;

        case 2:
          return Brushes.LightCyan;

        case 3:
          return Brushes.LightGoldenrodYellow;

        case 4:
          return Brushes.LightGray;

        case 5:
          return Brushes.LightGreen;

        case 6:
          return Brushes.LightPink;

        case 7:
          return Brushes.LightSalmon;

        case 8:
          return Brushes.LightSeaGreen;

        case 9:
          return Brushes.LightSkyBlue;

        case 10:
          return Brushes.LightSlateGray;

        case 11:
          return Brushes.LightSteelBlue;

        case 12:
          return Brushes.LightYellow;
      }
      return Brushes.LightBlue;
    }

    private Brush GetSeriesBrushesPastel(int i)
    {
      switch ((i % 0x1a))
      {
        case 0:
          return Brushes.CadetBlue;

        case 1:
          return Brushes.MediumTurquoise;

        case 2:
          return Brushes.Aquamarine;

        case 3:
          return Brushes.LightCyan;

        case 4:
          return Brushes.Azure;

        case 5:
          return Brushes.AliceBlue;

        case 6:
          return Brushes.MintCream;

        case 7:
          return Brushes.DarkSeaGreen;

        case 8:
          return Brushes.PaleGreen;

        case 9:
          return Brushes.LightGreen;

        case 10:
          return Brushes.MediumPurple;

        case 11:
          return Brushes.CornflowerBlue;

        case 12:
          return Brushes.Lavender;

        case 13:
          return Brushes.GhostWhite;

        case 14:
          return Brushes.PaleGoldenrod;

        case 15:
          return Brushes.LightGoldenrodYellow;

        case 0x10:
          return Brushes.LemonChiffon;

        case 0x11:
          return Brushes.LightYellow;

        case 0x12:
          return Brushes.Orchid;

        case 0x13:
          return Brushes.Plum;

        case 20:
          return Brushes.LightPink;

        case 0x15:
          return Brushes.Pink;

        case 0x16:
          return Brushes.LavenderBlush;

        case 0x17:
          return Brushes.Linen;

        case 0x18:
          return Brushes.PaleTurquoise;

        case 0x19:
          return Brushes.OldLace;
      }
      return Brushes.CadetBlue;
    }

    private Brush GetSeriesBrushesPatterned(int i)
    {
      switch ((i % 10))
      {
        case 0:
          return new HatchBrush(HatchStyle.LargeConfetti, Color.Blue, Color.White);

        case 1:
          return new HatchBrush(HatchStyle.Cross, Color.Red, Color.White);

        case 2:
          return new HatchBrush(HatchStyle.DarkDownwardDiagonal, Color.Green, Color.White);

        case 3:
          return new HatchBrush(HatchStyle.OutlinedDiamond, Color.Purple, Color.White);

        case 4:
          return new HatchBrush(HatchStyle.DarkHorizontal, Color.DeepSkyBlue, Color.White);

        case 5:
          return new HatchBrush(HatchStyle.SmallConfetti, Color.Orange, Color.White);

        case 6:
          return new HatchBrush(HatchStyle.HorizontalBrick, Color.Magenta, Color.White);

        case 7:
          return new HatchBrush(HatchStyle.LargeCheckerBoard, Color.Gold, Color.White);

        case 8:
          return new HatchBrush(HatchStyle.Vertical, Color.Lime, Color.White);

        case 9:
          return new HatchBrush(HatchStyle.SolidDiamond, Color.Teal, Color.White);

        case 10:
          return new HatchBrush(HatchStyle.DiagonalBrick, Color.Pink, Color.White);
      }
      return new HatchBrush(HatchStyle.BackwardDiagonal, Color.Blue, Color.White);
    }

    private Brush GetSeriesBrushesPatternedBlack(int i)
    {
      switch ((i % 10))
      {
        case 0:
          return new HatchBrush(HatchStyle.LargeConfetti, Color.Black, Color.White);

        case 1:
          return new HatchBrush(HatchStyle.Weave, Color.Black, Color.White);

        case 2:
          return new HatchBrush(HatchStyle.DarkDownwardDiagonal, Color.Black, Color.White);

        case 3:
          return new HatchBrush(HatchStyle.OutlinedDiamond, Color.Black, Color.White);

        case 4:
          return new HatchBrush(HatchStyle.DarkHorizontal, Color.Black, Color.White);

        case 5:
          return new HatchBrush(HatchStyle.SmallConfetti, Color.Black, Color.White);

        case 6:
          return new HatchBrush(HatchStyle.HorizontalBrick, Color.Black, Color.White);

        case 7:
          return new HatchBrush(HatchStyle.Wave, Color.Black, Color.White);

        case 8:
          return new HatchBrush(HatchStyle.Vertical, Color.Black, Color.White);

        case 9:
          return new HatchBrush(HatchStyle.SolidDiamond, Color.Black, Color.White);

        case 10:
          return new HatchBrush(HatchStyle.DiagonalBrick, Color.Black, Color.White);
      }
      return new HatchBrush(HatchStyle.BackwardDiagonal, Color.Black, Color.White);
    }

    protected ChartMarkerEnum[] GetSeriesMarkers()
    {
      ChartMarkerEnum[] enumArray = new ChartMarkerEnum[this.SeriesCount];
      for (int i = 0; i < this.SeriesCount; i++)
      {
        enumArray[i] = (ChartMarkerEnum)(i % 6);
      }
      return enumArray;
    }

    protected string GetSeriesValue(Report rpt, int iCol)
    {
      Row row;
      MatrixCellEntry entry = this._DataDefn[0, iCol];
      if (entry.Data.Data.Count > 0)
      {
        row = entry.Data.Data[0];
      }
      else
      {
        row = null;
      }
      ChartExpression displayItem = (ChartExpression)entry.DisplayItem;
      return ((displayItem.ChartLabel == null) ? displayItem.Value.EvaluateString(rpt, row) : displayItem.ChartLabel.EvaluateString(rpt, row));
    }

    protected void GetValueMaxMin(Report rpt, ref double max, ref double min, int xyb, int WhichYAxis)
    {
      if (((ChartSubTypeEnum)Enum.Parse(typeof(ChartSubTypeEnum), this._ChartDefn.Subtype.EvaluateString(rpt, this._row))) == ChartSubTypeEnum.PercentStacked)
      {
        max = 1.0;
        min = 0.0;
      }
      else
      {
        Axis axis;
        double num;
        double num2;
        if ((this._ChartDefn.Type == ChartTypeEnum.Bubble) || (this._ChartDefn.Type == ChartTypeEnum.Scatter))
        {
          if (xyb == 0)
          {
            axis = (this._ChartDefn.CategoryAxis != null) ? this._ChartDefn.CategoryAxis.Axis : null;
          }
          else if (xyb == 1)
          {
            axis = (this._ChartDefn.ValueAxis != null) ? this._ChartDefn.ValueAxis.Axis : null;
          }
          else
          {
            axis = null;
          }
        }
        else
        {
          axis = (this._ChartDefn.ValueAxis != null) ? this._ChartDefn.ValueAxis.Axis : null;
        }
        if (axis != null)
        {
          num = axis.MaxEval(rpt, this._row);
          num2 = axis.MinEval(rpt, this._row);
        }
        else
        {
          num = num2 = -2147483648.0;
        }
        if (!((((num == -2147483648.0) || num.Equals(double.NaN)) || (num2 == -2147483648.0)) || num2.Equals(double.NaN)))
        {
          max = num;
          min = num2;
        }
        else
        {
          this.GetMaxMinDataValue(rpt, out max, out min, xyb, 1);
          if (!((num == -2147483648.0) || num.Equals(double.NaN)))
          {
            max = num;
          }
          else
          {
            this._gridIncrs = 10;
            double num3 = max / ((double)this._gridIncrs);
            double y = Math.Floor(Math.Log10(Math.Abs(num3)));
            double num5 = Math.Pow(10.0, y) * Math.Sign(max);
            double num6 = (int)((num3 / num5) + 0.5);
            if (num6 > 5.0)
            {
              num6 = 10.0;
            }
            else if (num6 > 2.0)
            {
              num6 = 5.0;
            }
            else if (num6 > 1.0)
            {
              num6 = 2.0;
            }
            while (max < ((num6 * num5) * this._gridIncrs))
            {
              this._gridIncrs--;
            }
            while (max > ((num6 * num5) * this._gridIncrs))
            {
              this._gridIncrs++;
            }
            double num7 = max;
            max = (num6 * num5) * this._gridIncrs;
            if (num7 > (max - ((max / ((double)this._gridIncrs)) * 0.5)))
            {
              max += max / ((double)this._gridIncrs);
              this._gridIncrs++;
            }
          }
          if (!((num2 == -2147483648.0) || num2.Equals(double.NaN)))
          {
            min = num2;
          }
          else if (min > 0.0)
          {
            min = 0.0;
          }
          else
          {
            min = Math.Floor(min);
          }
        }
      }
    }

    protected string GetYAxis(Report rpt, int row, int col)
    {
      try
      {
        if (this is ChartColumn)
        {
          return ((ChartExpression)this._DataDefn[col, row].DisplayItem).YAxis.Source;
        }
      }
      catch
      {
      }
      return "Left";
    }

    internal Metafile Image(Report rpt)
    {
      if (this._bm == null)
      {
        this.Draw(rpt);
      }
      return this._mf;
    }

    protected bool IsLegendBottom()
    {
      Legend legend = this._ChartDefn.Legend;
      if ((legend != null) && legend.Visible)
      {
        switch (legend.Position)
        {
          case LegendPositionEnum.LeftBottom:
          case LegendPositionEnum.RightBottom:
          case LegendPositionEnum.BottomRight:
          case LegendPositionEnum.BottomCenter:
          case LegendPositionEnum.BottomLeft:
            return true;
        }
      }
      return false;
    }

    protected bool IsLegendInsidePlotArea()
    {
      Legend legend = this._ChartDefn.Legend;
      if (!((legend != null) && legend.Visible))
      {
        return false;
      }
      return legend.InsidePlotArea;
    }

    protected bool IsLegendLeft()
    {
      Legend legend = this._ChartDefn.Legend;
      if ((legend != null) && legend.Visible)
      {
        switch (legend.Position)
        {
          case LegendPositionEnum.TopLeft:
          case LegendPositionEnum.LeftTop:
          case LegendPositionEnum.LeftCenter:
          case LegendPositionEnum.LeftBottom:
          case LegendPositionEnum.BottomLeft:
            return true;
        }
      }
      return false;
    }

    protected bool IsLegendRight()
    {
      Legend legend = this._ChartDefn.Legend;
      if ((legend != null) && legend.Visible)
      {
        switch (legend.Position)
        {
          case LegendPositionEnum.TopRight:
          case LegendPositionEnum.RightTop:
          case LegendPositionEnum.RightCenter:
          case LegendPositionEnum.RightBottom:
          case LegendPositionEnum.BottomRight:
            return true;
        }
      }
      return false;
    }

    protected bool IsLegendTop()
    {
      Legend legend = this._ChartDefn.Legend;
      if ((legend != null) && legend.Visible)
      {
        switch (legend.Position)
        {
          case LegendPositionEnum.TopLeft:
          case LegendPositionEnum.TopCenter:
          case LegendPositionEnum.TopRight:
          case LegendPositionEnum.LeftTop:
          case LegendPositionEnum.RightTop:
            return true;
        }
      }
      return false;
    }

    internal void Save(Report rpt, Stream stream, ImageFormat im)
    {
      if (this._bm == null)
      {
        this.Draw(rpt);
      }
      this._mf.Save(stream, im);
    }

    protected Brush[] SeriesBrush(Report rpt, Row row, ReportDefn defn)
    {
      if (this._SeriesBrush == null)
      {
        this._SeriesBrush = this.GetSeriesBrushes(rpt, row, defn);
      }
      return this._SeriesBrush;
    }

    protected void SetIncrementAndInterval(Report rpt, Axis a, double min, double max, out double incr, out int interval)
    {
      interval = this._gridIncrs;
      if (a.MajorInterval != null)
      {
        incr = a.MajorInterval.EvaluateDouble(rpt, this.ChartRow);
        if (((double)incr).CompareTo(double.MinValue) == 0)
        {
          incr = (max - min) / ((double)interval);
        }
        else
        {
          interval = (int)(Math.Abs((double)(max - min)) / incr);
        }
      }
      else
      {
        incr = (max - min) / ((double)interval);
      }
    }

    protected bool ShowRightYAxis(Report rpt)
    {
      for (int i = 1; i <= this.CategoryCount; i++)
      {
        for (int j = 1; j <= this.SeriesCount; j++)
        {
          if (this.GetYAxis(rpt, j, 1).ToUpper() == "RIGHT")
          {
            return true;
          }
        }
      }
      return false;
    }

    protected int AxisTickMarkMajorLen
    {
      get
      {
        return 6;
      }
    }

    protected int AxisTickMarkMinorLen
    {
      get
      {
        return 3;
      }
    }

    protected int CategoryCount
    {
      get
      {
        return (this._DataDefn.GetLength(0) - 1);
      }
    }

    protected Chart ChartDefn
    {
      get
      {
        return this._ChartDefn;
      }
    }

    protected Row ChartRow
    {
      get
      {
        return this._row;
      }
    }

    protected MatrixCellEntry[,] DataDefn
    {
      get
      {
        return this._DataDefn;
      }
    }

    protected int SeriesCount
    {
      get
      {
        return (this._DataDefn.GetLength(1) - 1);
      }
    }

    protected ChartMarkerEnum[] SeriesMarker
    {
      get
      {
        if (this._SeriesMarker == null)
        {
          this._SeriesMarker = this.GetSeriesMarkers();
        }
        return this._SeriesMarker;
      }
    }
  }
}

