﻿' R- Instat
' Copyright (C) 2015-2017
'
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License 
' along with this program.  If not, see <http://www.gnu.org/licenses/>.

Imports System.ComponentModel
Imports instat.Translations
Imports RDotNet
Imports unvell.ReoGrid
Imports unvell.ReoGrid.Events

Public Class sdgClimaticDataEntry
    Private lstColumnNames As New List(Of KeyValuePair(Of String, String()))

    ''' <summary>
    ''' stores the row indices changed in the grid 
    ''' key = grid row index, value = grid row name (which should always be a number)
    ''' </summary>
    Private dctRowsChanged As New Dictionary(Of Integer, Integer)

    'the current worksheet in the grid
    Private WithEvents grdCurrentWorkSheet As Worksheet

    Private lstNonEditableColumns As New List(Of String)

    Private strDataFrameName As String
    Private clsSaveDataEntryFunction As RFunction
    Private clsEditDataFrameFunction As RFunction
    Private clsGetKeyFunction As RFunction
    Private clsCommentsListFunction As RFunction
    Private clsListFunction As RFunction
    Private dfEditData As DataFrame
    Private strDateName As String
    Private strDateNameToBeRemoved As String
    Private lstElementsNames As List(Of String)
    Private lstElementsNamesToBeRemoved As New List(Of String)
    Private lstViewVariablesNames As List(Of String)
    Private strStationColumnName As String
    Private dDefaultValue As Double
    Private bNoDecimal As Boolean
    Private bAllowTrace As Boolean
    Private bTransform As Boolean
    Private bDefaultValue As Boolean
    Private dTranformValue As Double
    Private bFirstLoad As Boolean = True
    'used to check if current options allow the grid to be editable
    Private bAllowEdits As Boolean = True
    Private bResetCommentsSubdialog As Boolean
    Private strEntryType As String = ""
    Private iMonthlyTotalsColIndex As Integer
    Private ucrBaseSelector As ucrSelector

    Private Sub sdgClimaticDataEntry_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If bFirstLoad Then
            InitialiseControls()
            bFirstLoad = False
        End If
        autoTranslate(Me)
    End Sub

    Private Sub InitialiseControls()
        ucrChkAddFlagFieldData.SetParameter(New RParameter("add_flags", 2))
        ucrChkAddFlagFieldData.SetText("Add flag fields")
        ucrChkAddFlagFieldData.SetRDefault("FALSE")
    End Sub

    ''' <summary>
    ''' returns the data changed for the passed column as an R vector string
    ''' </summary>
    ''' <returns></returns>
    Public Function GetRowsChangedAsRVectorString(strColumnName As String, Optional strQuotes As String = "") As String
        Dim strValues As String = ""
        Dim iColumnIndex As Integer = 0
        Dim bEditableColumn As Boolean
        Dim newValue As String

        If grdCurrentWorkSheet Is Nothing Then
            Return "c()"
        End If

        For i As Integer = 0 To grdCurrentWorkSheet.ColumnCount - 1
            If grdCurrentWorkSheet.ColumnHeaders.Item(i).Text = strColumnName Then
                iColumnIndex = i
                Exit For
            End If
        Next

        bEditableColumn = Not lstNonEditableColumns.Contains(strColumnName)

        For Each iRowIndex As Integer In dctRowsChanged.Keys
            newValue = grdCurrentWorkSheet.Item(row:=iRowIndex, col:=iColumnIndex)
            'for editable columns
            If bEditableColumn Then
                If bAllowTrace AndAlso newValue.ToUpper = "T" Then
                    newValue = 0.03
                ElseIf bTransform And IsNumeric(newValue) Then
                    newValue = newValue * dTranformValue
                End If
            End If

            If strValues = "" Then
                strValues = strQuotes & newValue & strQuotes
            Else
                strValues = strValues & "," & strQuotes & newValue & strQuotes
            End If
        Next

        Return "c(" & strValues & ")"
    End Function

    Public Function GetRowNamesChangedAsRVectorString() As String
        Return "c(" & String.Join(",", dctRowsChanged.Values.ToArray) & ")"
    End Function

    Public Function NRowsChanged() As Integer
        Return dctRowsChanged.Count
    End Function

    Public Sub Reset()
        dctRowsChanged.Clear()
        grdCurrentWorkSheet = Nothing
    End Sub

    Public Sub Setup(dfEditData As DataFrame, strDataFrameName As String, clsSaveDataEntry As RFunction,
                     clsEditDataFrame As RFunction, clsNewGetKey As RFunction, clsNewCommentsList As RFunction, clsNewList As RFunction,
                     strDateName As String, lstElementsNames As List(Of String), dTranformValue As Double, dDefaultValue As Double,
                     Optional lstViewVariablesNames As List(Of String) = Nothing, Optional strStationColumnName As String = "",
                     Optional bDefaultValue As Boolean = False, Optional bNoDecimal As Boolean = False,
                     Optional bAllowTrace As Boolean = False, Optional bTransform As Boolean = False, Optional MissingValueAsNA As Boolean = False,
                     Optional strEntryType As String = "", Optional ucrNewBaseSelector As ucrSelector = Nothing, Optional bReset As Boolean = False)

        Dim arrColumnHeaders As String()
        clsGetKeyFunction = clsNewGetKey
        clsCommentsListFunction = clsNewCommentsList
        clsListFunction = clsNewList

        grdDataEntry.Worksheets.Clear()
        dctRowsChanged.Clear()
        lstNonEditableColumns.Clear()
        bResetCommentsSubdialog = bReset
        ucrBaseSelector = ucrNewBaseSelector

        Me.strDataFrameName = strDataFrameName
        Me.clsSaveDataEntryFunction = clsSaveDataEntry
        Me.clsEditDataFrameFunction = clsEditDataFrame
        Me.dfEditData = dfEditData
        Me.strDateName = strDateName
        Me.lstElementsNames = lstElementsNames
        Me.lstViewVariablesNames = lstViewVariablesNames
        Me.strStationColumnName = strStationColumnName
        Me.dDefaultValue = dDefaultValue
        Me.bNoDecimal = bNoDecimal
        Me.bAllowTrace = bAllowTrace
        Me.bTransform = bTransform
        Me.dTranformValue = dTranformValue
        Me.strEntryType = strEntryType
        Me.bDefaultValue = bDefaultValue


        ucrChkAddFlagFieldData.SetRCode(clsSaveDataEntryFunction, bReset, bCloneIfNeeded:=True)

        bAllowEdits = True
        cmdTransform.Text = "Transform"
        cmdTransform.Enabled = bTransform

        If Not strStationColumnName = "" Then
            lstNonEditableColumns.Add(strStationColumnName)
        End If
        lstNonEditableColumns.Add(strDateName)
        If lstViewVariablesNames IsNot Nothing Then
            For Each strVar In lstViewVariablesNames
                ' Note this is the format used in get_data_entry_data R method
                lstNonEditableColumns.Add(strVar & " (view)")
            Next
        End If

        grdCurrentWorkSheet = grdDataEntry.CreateWorksheet(strDataFrameName)
        arrColumnHeaders = dfEditData.ColumnNames

        grdCurrentWorkSheet.Columns = arrColumnHeaders.Count
        For k = 0 To arrColumnHeaders.Count - 1
            grdCurrentWorkSheet.ColumnHeaders.Item(k).Text = arrColumnHeaders(k)
            If Not lstElementsNames.Contains(arrColumnHeaders(k)) Then
                grdCurrentWorkSheet.ColumnHeaders(k).TextColor = Graphics.SolidColor.Black
            End If
        Next

        Dim dfValue As String
        Dim bNonEditableCell As Boolean

        'set grid rows
        grdCurrentWorkSheet.Rows = dfEditData.RowCount
        For i As Integer = 0 To dfEditData.RowCount - 1
            For j = 0 To grdCurrentWorkSheet.Columns - 1
                bNonEditableCell = lstNonEditableColumns.Contains(arrColumnHeaders(j))
                dfValue = dfEditData.Item(i, j)
                If dfValue = "NaN" Then
                    If MissingValueAsNA Then
                        dfValue = "NA"
                    Else
                        dfValue = ""
                    End If
                End If
                If bDefaultValue AndAlso Not bNonEditableCell Then
                    dfValue = dDefaultValue
                End If

                grdCurrentWorkSheet.Item(row:=i, col:=j) = dfValue

                If bDefaultValue AndAlso Not bNonEditableCell Then
                    grdCurrentWorkSheet.GetCell(i, j).Style.BackColor = Color.LightYellow
                End If

                If bNonEditableCell Then
                    grdCurrentWorkSheet.GetCell(i, j).IsReadOnly = True
                End If
            Next
            grdCurrentWorkSheet.RowHeaders.Item(i).Text = dfEditData.RowNames(i)
        Next

        For i As Integer = 0 To grdCurrentWorkSheet.RowCount - 1
            If bDefaultValue Then
                AddChangedRow(i)
            End If
        Next

        'if entry by month then, add the monthly totals rows and set the calculated totals
        If Me.strEntryType = "Month" Then
            'for monthly entry add 3 extra rows for; sum, calculated and difference
            grdCurrentWorkSheet.AppendRows(3)

            Dim iLastRowIndex As Integer = grdCurrentWorkSheet.Rows - 1
            iMonthlyTotalsColIndex = Array.IndexOf(arrColumnHeaders, Me.strDateName)

            grdCurrentWorkSheet.Item(row:=iLastRowIndex - 2, col:=iMonthlyTotalsColIndex) = "Sum"
            grdCurrentWorkSheet.Item(row:=iLastRowIndex - 1, col:=iMonthlyTotalsColIndex) = "Calculated"
            grdCurrentWorkSheet.Item(row:=iLastRowIndex, col:=iMonthlyTotalsColIndex) = "Difference"

            'set the monthly totals from the set rows
            For i As Integer = 0 To iLastRowIndex - 3
                For j = iMonthlyTotalsColIndex + 1 To grdCurrentWorkSheet.Columns - 1
                    'dont set totals for non editable columns
                    If Not lstNonEditableColumns.Contains(arrColumnHeaders(j)) Then
                        SetMonthlyTotalsRows(i, j)
                    End If
                Next
            Next

            'set the non editable cells for the added rows
            For i As Integer = iLastRowIndex - 3 To iLastRowIndex
                For j = 0 To grdCurrentWorkSheet.Columns - 1
                    'create the cell by setting the value if it does not exis
                    If grdCurrentWorkSheet.GetCell(row:=i, col:=j) Is Nothing Then
                        grdCurrentWorkSheet.Item(row:=i, col:=j) = ""
                    End If
                    'all calculated and difference cells should be uneditable
                    If i = iLastRowIndex - 1 OrElse i = iLastRowIndex Then
                        grdCurrentWorkSheet.GetCell(row:=i, col:=j).IsReadOnly = True
                        Continue For
                    End If

                    If lstNonEditableColumns.Contains(arrColumnHeaders(j)) Then
                        grdCurrentWorkSheet.GetCell(row:=i, col:=j).IsReadOnly = True
                    End If
                Next
            Next


        End If

        grdCurrentWorkSheet.SetRangeDataFormat(New RangePosition(0, 0, grdCurrentWorkSheet.Rows, grdCurrentWorkSheet.Columns), DataFormat.CellDataFormatFlag.Text)
        grdCurrentWorkSheet.SelectionForwardDirection = unvell.ReoGrid.SelectionForwardDirection.Down
        grdCurrentWorkSheet.SetSettings(unvell.ReoGrid.WorksheetSettings.Edit_DragSelectionToMoveCells, False)
        grdCurrentWorkSheet.SetSettings(unvell.ReoGrid.WorksheetSettings.Edit_DragSelectionToFillSerial, False)

        grdDataEntry.AddWorksheet(grdCurrentWorkSheet)
        grdDataEntry.SheetTabNewButtonVisible = False

        SetColumnNames(strDataFrameName, dfEditData.ColumnNames())

        ttCmdReset.SetToolTip(cmdReset, "Clears all data entry.")
        ttCmdTransformButton.SetToolTip(cmdTransform, "When implemented, this is an option to show the transformed data.")
    End Sub

    Public Sub SetColumnNames(strDataFrameName As String, strColumnNames As String())
        Dim iIndex As Integer
        iIndex = lstColumnNames.FindIndex(Function(x) x.Key = strDataFrameName)
        If iIndex <> -1 Then
            lstColumnNames.RemoveAt(iIndex)
        End If
        lstColumnNames.Add(New KeyValuePair(Of String, String())(strDataFrameName, strColumnNames))
    End Sub

    Private Function SelectedColumnsAsArray() As String()
        Dim strSelectedColumns As String()
        Dim lstCurrentDataColumns As String()

        lstCurrentDataColumns = lstColumnNames.Find(Function(x) x.Key = grdDataEntry.CurrentWorksheet.Name).Value

        If lstColumnNames IsNot Nothing AndAlso lstColumnNames.Count > 0 Then
            strSelectedColumns = New String(grdDataEntry.CurrentWorksheet.SelectionRange.Cols - 1) {}
            For i As Integer = 0 To grdDataEntry.CurrentWorksheet.SelectionRange.Cols - 1
                strSelectedColumns(i) = lstCurrentDataColumns(i + grdDataEntry.CurrentWorksheet.SelectionRange.Col)
            Next
            Return strSelectedColumns
        Else
            strSelectedColumns = New String() {}
        End If
        Return strSelectedColumns
    End Function

    Private Function GetFirstSelectedRow() As String
        Return grdCurrentWorkSheet.RowHeaders.Item(grdDataEntry.CurrentWorksheet.SelectionRange.Row).Text
    End Function

    Private Sub grdCurrSheet_BeforeCellEdit(sender As Object, e As CellBeforeEditEventArgs) Handles grdCurrentWorkSheet.BeforeCellEdit
        ''todo. do this disabling of data entry be done when setting up the grid. Not here
        'If lstNonEditableColumns.Contains(grdCurrentWorkSheet.ColumnHeaders(e.Cell.Column).Text) Then
        '    e.IsCancelled = True
        'End If
        'If InStr(grdCurrentWorkSheet.ColumnHeaders(e.Cell.Column).Text, "(view)") Then
        '    e.IsCancelled = True
        'End If
    End Sub

    Private Sub grdCurrSheet_BeforeCellKeyDown(sender As Object, e As BeforeCellKeyDownEventArgs) Handles grdCurrentWorkSheet.BeforeCellKeyDown
        If e.KeyCode = unvell.ReoGrid.Interaction.KeyCode.Delete OrElse e.KeyCode = unvell.ReoGrid.Interaction.KeyCode.Back Then
            MsgBox("Deleting cells is disabled." & Environment.NewLine & "To remove a cell's value, replace the value with NA.", MsgBoxStyle.Information, "Cannot delete cells.")
            e.IsCancelled = True
        End If
    End Sub

    Private Sub grdCurrentWorkSheet_BeforePaste(sender As Object, e As BeforeRangeOperationEventArgs) Handles grdCurrentWorkSheet.BeforePaste
        MsgBox("Pasting not yet implemented.", MsgBoxStyle.Information, "Pasting not implemented.")
        e.IsCancelled = True
    End Sub

    Private Sub grdCurrSheet_AfterCellEdit(sender As Object, e As CellAfterEditEventArgs) Handles grdCurrentWorkSheet.AfterCellEdit
        Dim bValidValue As Boolean = True
        Dim newValue As String = e.NewData

        If Not bAllowEdits Then
            'todo. set a better feedback message 
            MsgBox("Edits not allowed", MsgBoxStyle.Information, "No edits allowed.")
            e.EndReason = EndEditReason.Cancel
            Exit Sub
        End If

        If Not IsNumeric(newValue) AndAlso Not newValue = "NA" Then
            If Not (bAllowTrace AndAlso newValue.ToUpper = "T") Then
                MsgBox("Value is not numeric or NA.", MsgBoxStyle.Information, "Not numeric.")
                bValidValue = False
            End If
        ElseIf bNoDecimal AndAlso newValue.Contains(".") Then
            MsgBox("Value should not be decimal otherwise uncheck No Decimal.", MsgBoxStyle.Information, "Not decimal Allowed.")
            bValidValue = False
        End If

        If bValidValue Then
            'dont add any change in the last 3 rows if entry is by "Month"
            If Not (strEntryType = "Month" AndAlso e.Cell.Row >= grdCurrentWorkSheet.Rows - 3) Then
                AddChangedRow(e.Cell.Row)
            End If
            SetMonthlyTotalsRows(e.Cell.Row, e.Cell.Column, newValue)
            grdCurrentWorkSheet.GetCell(e.Cell.Row, e.Cell.Column).Style.BackColor = Color.Yellow
        Else
            e.EndReason = EndEditReason.Cancel
            'grdCurrentWorkSheet.FocusPos = New CellPosition(e.Cell.Address)
        End If

    End Sub

    ''' <summary>
    ''' adds the row index changed to the list of rows changes
    ''' </summary>
    ''' <param name="iRow"> row index</param>
    Private Sub AddChangedRow(iRow As Integer)
        'add the row index and row name to the last of rows changed
        If Not dctRowsChanged.ContainsKey(iRow) Then
            dctRowsChanged.Add(iRow, Integer.Parse(grdCurrentWorkSheet.RowHeaders.Item(iRow).Text))
        End If
    End Sub

    Private Sub cmdReset_Click(sender As Object, e As EventArgs) Handles cmdReset.Click
        If MsgBox("All data entry will be lost. Are you sure you want to continue?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            clsSaveDataEntryFunction.RemoveParameterByName("rows_changed")
            Setup(dfEditData:=dfEditData, strDataFrameName:=strDataFrameName, clsSaveDataEntry:=clsSaveDataEntryFunction,
                     clsEditDataFrame:=clsEditDataFrameFunction, clsNewGetKey:=clsGetKeyFunction, clsNewCommentsList:=clsCommentsListFunction,
                     clsNewList:=clsListFunction,
                     strDateName:=strDateName, lstElementsNames:=lstElementsNames, dTranformValue:=dTranformValue, dDefaultValue:=dDefaultValue,
                      lstViewVariablesNames:=lstViewVariablesNames, strStationColumnName:=strStationColumnName,
                      bDefaultValue:=bDefaultValue)
        End If
    End Sub

    Private Sub ucrSdgBaseButtons_ClickReturn(sender As Object, e As EventArgs) Handles ucrSdgBaseButtons.ClickReturn
        Dim i As Integer

        clsEditDataFrameFunction.RemoveParameterByName(strDateNameToBeRemoved)
        For Each strElementName As String In lstElementsNamesToBeRemoved
            clsEditDataFrameFunction.RemoveParameterByName(strElementName)
        Next

        If NRowsChanged() > 0 Then
            strDateNameToBeRemoved = strDateName
            lstElementsNamesToBeRemoved = lstElementsNames
            clsEditDataFrameFunction.AddParameter(strDateName, "as.Date(" & GetRowsChangedAsRVectorString(strDateName, Chr(34)) & ")", iPosition:=1)
            i = 2
            For Each strElementName As String In lstElementsNames
                clsEditDataFrameFunction.AddParameter(strElementName, GetRowsChangedAsRVectorString(strElementName), iPosition:=i)
                i = i + 1
            Next
            clsSaveDataEntryFunction.AddParameter("rows_changed", GetRowNamesChangedAsRVectorString(), iPosition:=2)
            clsSaveDataEntryFunction.AddParameter("comments_list", clsRFunctionParameter:=clsListFunction, iPosition:=3)
        Else
            clsSaveDataEntryFunction.RemoveParameterByName("rows_changed")
            clsSaveDataEntryFunction.RemoveParameterByName("comments_list")
        End If
    End Sub

    Private Sub cmdTransform_Click(sender As Object, e As EventArgs) Handles cmdTransform.Click
        'todo. check how translation will affect this, possibly use 2 buttons instead of one ?
        If cmdTransform.Text = "Transform" Then
            cmdTransform.Text = "UnTransform"
            bAllowEdits = False
        Else
            cmdTransform.Text = "Transform"
            bAllowEdits = True
        End If
    End Sub

    Private Sub cmdComment_Click(sender As Object, e As EventArgs) Handles cmdComment.Click
        sdgCommentForDataEntry.SetUpCommentsSubdialog(clsNewSaveDataEntry:=clsSaveDataEntryFunction, clsNewGetKey:=clsGetKeyFunction,
                      clsNewCommentsList:=clsCommentsListFunction, clsNewList:=clsListFunction, strDataFrame:=grdCurrentWorkSheet.Name, strRow:=GetFirstSelectedRow(),
                      strColumn:=SelectedColumnsAsArray()(0), ucrNewBaseSelector:=ucrBaseSelector, bReset:=bResetCommentsSubdialog)
        sdgCommentForDataEntry.ShowDialog()
        bResetCommentsSubdialog = False
    End Sub

    ''' <summary>
    ''' sets the monthly totals; the last 3 rows in the grid
    ''' </summary>
    ''' <param name="iRowIndexSelected"></param>
    ''' <param name="iColIndexSelected"></param>
    ''' <param name="strNewValue"></param>
    Private Sub SetMonthlyTotalsRows(iRowIndexSelected As Integer, iColIndexSelected As Integer, Optional strNewValue As String = Nothing)
        Dim iLastRowIndex As Integer = grdCurrentWorkSheet.Rows - 1
        Dim strSumValue As String
        Dim strValue As String
        Dim dTotalCalculatedValue As Double
        Dim dDifferenceValue As Double

        'only do monthly totals for columns after the monthly total column
        'If Me.strEntryType <> "Month" OrElse iColIndexSelected <= iMonthlyTotalsColIndex Then
        '    Exit Sub
        'End If

        'only do monthly totals for columns after the monthly total column
        'also exit if its a "calculated" or "difference" row
        If Me.strEntryType <> "Month" OrElse iColIndexSelected <= iMonthlyTotalsColIndex OrElse iRowIndexSelected = iLastRowIndex - 1 OrElse iRowIndexSelected = iLastRowIndex Then
            Exit Sub
        End If

        'get user input sum value, if the new value is a "sum" value then just use the new value
        If iRowIndexSelected = iLastRowIndex - 2 AndAlso strNewValue IsNot Nothing Then
            strSumValue = strNewValue
        Else
            strSumValue = grdCurrentWorkSheet.Item(row:=iLastRowIndex - 2, col:=iColIndexSelected)
        End If

        'calculate the "calculated" value
        For i As Integer = 0 To iLastRowIndex - 3 'exclude the 3 rows; sum, calculated and difference
            'if new value of the current cell row item is there
            'then just use the new value to get the calculated value
            If iRowIndexSelected = i AndAlso strNewValue IsNot Nothing Then
                strValue = strNewValue
            Else
                strValue = grdCurrentWorkSheet.Item(row:=i, col:=iColIndexSelected)
            End If

            If IsNumeric(strValue) Then
                dTotalCalculatedValue = dTotalCalculatedValue + Double.Parse(strValue)
            End If
        Next

        'set the calculated value and round of to 2 d.p
        dTotalCalculatedValue = Math.Round(dTotalCalculatedValue, 2)
        grdCurrentWorkSheet.Item(row:=iLastRowIndex - 1, col:=iColIndexSelected) = dTotalCalculatedValue

        'set difference value, only when there is a sum value else remove the difference
        If IsNumeric(strSumValue) Then
            dDifferenceValue = Math.Round(Double.Parse(strSumValue) - dTotalCalculatedValue, 2)
            grdCurrentWorkSheet.Item(row:=iLastRowIndex, col:=iColIndexSelected) = dDifferenceValue
            'grdCurrentWorkSheet.GetCell(row:=iLastRowIndex, col:=iColIndexSelected).Style.BackColor = If(dDifferenceValue = 0, Color.LightGreen, Color.White)
        Else
            grdCurrentWorkSheet.Item(row:=iLastRowIndex, col:=iColIndexSelected) = ""
            'grdCurrentWorkSheet.GetCell(row:=iLastRowIndex, col:=iColIndexSelected).Style.BackColor = Color.White
        End If

    End Sub
End Class