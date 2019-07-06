﻿'
' Based on the Source from Rui Godinho Lopes <email>rui@ruilopes.com</email>
' Link: https://www.codeproject.com/Articles/1822/Per-Pixel-Alpha-Blend-in-C
'
' Modified from original c# source into vb.net code
'   Then further modifications for WFInfo uses
'
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

Public Class PerPixelAlphaForm
    Inherits Form

    Friend WithEvents tHide As Timer

    Private components As System.ComponentModel.IContainer

    Public Sub New()
        InitializeComponent()
        FormBorderStyle = FormBorderStyle.None
        TopMost = True
        ShowInTaskbar = False
    End Sub

    Public Function TestBitmap(partName As String, wid As Integer) As Bitmap
        Dim hei As Integer = 100
        Dim pad As Integer = 15
        Dim curve As Integer = 20 * 2
        Dim textSep As Single = 10
        Dim oneLine As Single = 10

        Dim font As New System.Drawing.Font(privateFonts.Families(0), 12)
        Dim strFormat As New StringFormat()
        strFormat.Alignment = StringAlignment.Center


        Dim job As Newtonsoft.Json.Linq.JObject = db.market_data(partName)
        Dim volumeTxt As String = job("volume").ToString() & " sold last 48hrs"

        Using fake As New Bitmap(1, 1)
            Using g As Graphics = Graphics.FromImage(fake)

                textSep = g.MeasureString(partName, font, wid - pad * 2, strFormat).Height
                oneLine = g.MeasureString("text", font, wid - pad * 2, strFormat).Height
                hei = pad * 2 + textSep * 1.5 + oneLine * 2

            End Using
        End Using

        Dim layout As New RectangleF(pad, pad, wid - pad * 2, hei - pad * 2)

        Dim bmp As New Bitmap(wid, hei)

        Dim clrs As Color() = {Color.FromArgb(0, 0, 0, 0), Color.FromArgb(200, 0, 0, 0), Color.FromArgb(200, 0, 0, 0)}
        Dim segs As Single() = {0.0, pad * 2 / wid, 1.0}
        Dim blender As New ColorBlend()
        blender.Colors = clrs
        blender.Positions = segs

        Dim gp As New GraphicsPath()
        gp.StartFigure()

        ' 0,0 w/size 40,40      top-left
        gp.AddArc(0, 0, curve, curve, 180, 90)
        ' 60,0 w/size 40,40     top-right
        gp.AddArc(wid - curve, 0, curve, curve, -90, 90)
        ' 60,60 w/size 40,40    bottom-right
        gp.AddArc(wid - curve, hei - curve, curve, curve, 0, 90)
        ' 0,60 w/size 40,40     bottom-left
        gp.AddArc(0, hei - curve, curve, curve, 90, 90)

        gp.CloseFigure()

        Using g As Graphics = Graphics.FromImage(bmp)
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic

            Using brushie As New PathGradientBrush(gp)
                brushie.InterpolationColors = blender
                brushie.CenterPoint = New PointF(wid / 2, hei / 2)

                g.FillRectangle(brushie, 0, 0, wid, hei)
                'g.DrawRectangle(New Pen(Glob.rareBrush), New Rectangle(pad, pad, wid - pad * 2, hei - pad * 2))

                g.DrawString(partName, font, Glob.textBrush, layout, strFormat)
                layout.Y += textSep * 1.5

                Dim tempY As Single = layout.Y + oneLine / 20
                Dim tempDim As Single = oneLine * 0.8
                g.DrawImage(My.Resources.plat, CSng(pad * 2.25), tempY, tempDim, tempDim)
                g.DrawString(job("plat").ToString(), font, textBrush, CSng(pad * 2 + oneLine), layout.Y)
                g.DrawImage(My.Resources.ducat_w, CSng(wid / 2 + pad), tempY, tempDim, tempDim)
                g.DrawString(job("ducats").ToString(), font, textBrush, CSng(wid / 2 + pad / 1.25 + oneLine), layout.Y)


                'g.DrawString(job("plat"), font, Glob.textBrush, layout, strFormat)
                layout.Y += oneLine
                g.DrawString(volumeTxt, font, Glob.textBrush, layout, strFormat)

            End Using
        End Using

        Return bmp
    End Function


    Public Sub SetBitmap(bitmap As Bitmap)
        If bitmap.PixelFormat <> PixelFormat.Format32bppArgb Then
            Throw New ApplicationException("The bitmap must be 32ppp with alpha-channel.")
        End If

        Dim screenDc As IntPtr = GetDC(IntPtr.Zero)
        Dim memDc As IntPtr = CreateCompatibleDC(screenDc)
        Dim hBitmap As IntPtr = IntPtr.Zero
        Dim oldBitmap As IntPtr = IntPtr.Zero

        Try
            hBitmap = bitmap.GetHbitmap(Color.FromArgb(0))
            oldBitmap = SelectObject(memDc, hBitmap)

            Dim Size As New WinSize(bitmap.Width, bitmap.Height)
            Dim pointSource As New WinPoint(0, 0)
            Dim Blend As New BLENDFUNCTION With {
                .BlendOp = Win32.AC_SRC_OVER,
                .BlendFlags = 0,
                .SourceConstantAlpha = 255,
                .AlphaFormat = Win32.AC_SRC_ALPHA
            }

            Win32.UpdateLayeredWindow(Handle, screenDc, Nothing, Size, memDc, pointSource, 0, Blend, Win32.ULW_ALPHA)
        Finally
            Win32.ReleaseDC(IntPtr.Zero, screenDc)
            If hBitmap <> IntPtr.Zero Then
                Win32.SelectObject(memDc, oldBitmap)
                Win32.DeleteObject(hBitmap)
            End If
            Win32.DeleteDC(memDc)
        End Try
    End Sub

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or &H80000
            Return cp
        End Get
    End Property

    '("Loki Prime Systems", New Point(100, 100), 200)
    Public Sub ShowAtLocation(partName As String, pos As Point, wid As Integer)
        Me.Show()
        Me.SetBitmap(TestBitmap(partName, wid))
        Me.Location = pos
    End Sub

    Private Sub tHide_Tick(sender As Object, e As EventArgs) Handles tHide.Tick
        Me.Hide()
        tHide.Stop()
    End Sub

    Friend Sub DoTheThings()
        Me.Show()
        'Me.SetBitmap(TestBitmap("Loki Prime Systems"))
    End Sub

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.tHide = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'tHide
        '
        Me.tHide.Interval = 10000
        '
        'PerPixelAlphaForm
        '
        Me.ClientSize = New System.Drawing.Size(284, 262)
        Me.Name = "PerPixelAlphaForm"
        Me.ResumeLayout(False)

    End Sub
End Class