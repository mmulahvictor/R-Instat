﻿Imports instat

Public Class ucrDayOfYear
    ' Is the parameter value associated with the control a number
    ' If so then when the control value is e.g. 1 February the parameter value will be 32
    ' Otherwise, parameter value will be set as a string e.g. "1 February" or similar depending on string options
    Private bParameterIsNumber As Boolean = True
    Private bParameterIsString As Boolean = False
    ' If True uses 29 February is included and 31 December = 366
    ' Otherwise 29 December is not included and 31 December = 365
    Private b366DayOfYear As Boolean = True
    Private dtbMonths As DataTable
    Private bFirstLoad As Boolean = True
    Private strMonthsFull As String()
    Private strMonthsAbbreviated As String()
    Private bUpdate As Boolean = True

    Private Sub ucrDayOfYear_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim strDays(30) As String

        If bFirstLoad Then
            dtbMonths = New DataTable
            dtbMonths.Columns.Add("Number", GetType(Integer))
            dtbMonths.Columns.Add("Full", GetType(String))
            dtbMonths.Columns.Add("Abbreviated", GetType(String))
            'TODO should we use these instead of fixed English names?
            'System.Globalization.DateTimeFormatInfo.InvariantInfo.MonthNames
            'System.Globalization.DateTimeFormatInfo.InvariantInfo.AbbreviatedMonthNames
            strMonthsFull = {"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"}
            strMonthsAbbreviated = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"}
            For i As Integer = 0 To 11
                dtbMonths.Rows.Add(i, strMonthsFull(i), strMonthsAbbreviated(i))
            Next
            'TODO Display/Value member should be changeable
            ucrInputMonth.SetDataSource(dtbMonths, strDisplayMember:="Full", strValueMember:="Number")

            For i As Integer = 0 To 30
                strDays(i) = i + 1
            Next
            ucrInputDay.SetItems(strDays)

            ucrInputDay.bIsActiveRControl = False
            ucrInputMonth.bIsActiveRControl = False
            bFirstLoad = False
        End If
    End Sub

    Public Overrides Sub UpdateParameter(clsTempParam As RParameter)
        If bChangeParameterValue AndAlso clsTempParam IsNot Nothing AndAlso bUpdate Then
            clsTempParam.SetArgumentValue(GetValue())
        End If
    End Sub

    Public Sub SetParameterIsNumber()
        bParameterIsNumber = True
        bParameterIsString = False
    End Sub

    Public Sub SetParameterIsString()
        bParameterIsString = True
        bParameterIsNumber = False
    End Sub

    ' Returns a string to be used as the parameter value, depending on the options set e.g. 366 or "31 December" or "31/12"
    Public Function GetValue() As String
        If bParameterIsNumber Then
            Return DayOfYearNumber()
        ElseIf bParameterIsString Then
            'TODO allow options to determine this format
            Return ucrInputDay.GetText() & " " & ucrInputMonth.GetText()
        Else
            Return ""
        End If
    End Function

    Public Function DayOfYearNumber() As Integer
        Dim iYear As Integer
        Dim dtTemp As Date

        If b366DayOfYear Then
            iYear = 2000
        Else
            iYear = 1999
        End If
        dtTemp = New Date(year:=iYear, month:=ucrInputMonth.GetValue() + 1, day:=ucrInputDay.GetValue())
        Return dtTemp.DayOfYear
    End Function

    Protected Overrides Sub SetControlValue()
        Dim strDayOfYearNumber As String
        Dim iDayOfYearNumber As Integer
        Dim iYear As Integer
        Dim dtTemp As Date
        Dim strMonth As String
        Dim strDay As String
        Dim clsTempParameter As RParameter
        Dim bInvalid As Boolean = False

        clsTempParameter = GetParameter()
        If clsTempParameter IsNot Nothing Then
            If bChangeParameterValue Then
                If bParameterIsNumber Then
                    If clsTempParameter.bIsString Then
                        strDayOfYearNumber = clsTempParameter.strArgumentValue
                        If Integer.TryParse(strDayOfYearNumber, iDayOfYearNumber) Then
                            If b366DayOfYear Then
                                iYear = 2000
                                If iDayOfYearNumber < 1 OrElse iDayOfYearNumber > 366 Then
                                    bInvalid = True
                                End If
                            Else
                                iYear = 1999
                                If iDayOfYearNumber < 1 OrElse iDayOfYearNumber > 365 Then
                                    bInvalid = True
                                End If
                            End If
                            If Not bInvalid Then
                                dtTemp = New Date(year:=iYear, month:=1, day:=1).AddDays(iDayOfYearNumber - 1)
                                strDay = dtTemp.Day
                                strMonth = dtTemp.Month
                                bUpdate = False
                                ucrInputDay.SetName(dtTemp.Day)
                                'TODO this should be done through a method in ucrInputMonth
                                ucrInputMonth.cboInput.SelectedIndex = dtTemp.Month - 1
                                bUpdate = True
                                UpdateAllParameters()
                            End If
                        Else
                            bInvalid = True
                        End If
                    ElseIf clsTempParameter.bIsFunction OrElse clsTempParameter.bIsOperator Then
                        bInvalid = True
                    Else
                        'Clear? Reset?
                    End If
                Else
                    'TODO case where parameter isn't a number e.g. "14 Jan"
                End If
                If bInvalid Then
                    MsgBox("Developer error: Cannot set value of control: " & Name & ". Expecting parameter value to an R expression that can be interpreted as a day of the year")
                End If
            End If
        End If
    End Sub

    Public Overrides Sub SetRDefault(objNewDefault As Object)
        Dim iDefault As Integer()

        MyBase.SetRDefault(objNewDefault)
        iDefault = TryCast(objNewDefault, Integer())
        If Not (iDefault IsNot Nothing AndAlso iDefault.Count = 2) Then
            MsgBox("Developer error: Cannot set the default value of control " & Me.Name & ". The default but me a list of length 2 with day and month as integer.")
        End If
    End Sub

    Public Overrides Function IsRDefault() As Boolean
        Dim iDefault As Integer()

        iDefault = TryCast(objRDefault, Integer())

        If iDefault IsNot Nothing AndAlso iDefault.Count = 2 Then
            Return (iDefault(0) = ucrInputDay.GetText() & iDefault(1) = ucrInputMonth.GetText())
        Else
            Return False
        End If
    End Function

    Private Sub ucrInputDay_ControlContentsChanged(ucrChangedControl As ucrCore) Handles ucrInputDay.ControlContentsChanged
        OnControlContentsChanged()
    End Sub

    Private Sub ucrInputMonth_ControlContentsChanged(ucrChangedControl As ucrCore) Handles ucrInputMonth.ControlContentsChanged
        OnControlContentsChanged()
    End Sub

    Private Sub ucrInputDay_ControlValueChanged(ucrChangedControl As ucrCore) Handles ucrInputDay.ControlValueChanged
        OnControlValueChanged()
    End Sub

    Private Sub ucrInputMonth_ControlValueChanged(ucrChangedControl As ucrCore) Handles ucrInputMonth.ControlValueChanged
        bUpdate = False
        AdjustDay()
        bUpdate = True
        OnControlValueChanged()
    End Sub

    Private Sub AdjustDay()
        Dim iMonth As Integer
        Dim iDay As Integer
        Dim iMaxFebDay As Integer

        If b366DayOfYear Then
            iMaxFebDay = 29
        Else
            iMaxFebDay = 28
        End If
        If Integer.TryParse(ucrInputDay.GetValue(), iDay) Then
            If Integer.TryParse(ucrInputMonth.GetValue(), iMonth) Then
                If {4, 6, 9, 11}.Contains(iMonth) AndAlso iDay = 31 Then
                    ucrInputDay.SetName(30)
                ElseIf iMonth = 2 AndAlso iDay > iMaxFebDay Then
                    ucrInputDay.SetName(iMaxFebDay)
                End If
            End If
        End If
    End Sub
End Class
