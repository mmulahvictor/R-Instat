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

Imports instat.Translations
Imports RDotNet

Public Class sdgImportFromClimSoft
    Private clsRDatabaseConnect As RFunction
    Private clsRDatabaseDisconnect As RFunction
    Private clsRHasConnection As RFunction
    Private bFirstLoad As Boolean = True
    Private bConnected As Boolean = False

    Private Sub sdgImportFromClimSoft_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        autoTranslate(Me)
        If bFirstLoad Then
            InitialiseDialog()
            SetDefaults()
            bFirstLoad = False
        End If
        SetRCodeForControls()
        UpdateConnectionState()
    End Sub

    Private Sub InitialiseDialog()
        Dim dctDatabaseNames As New Dictionary(Of String, String)
        Dim dctPorts As New Dictionary(Of String, String)

        'database names
        dctDatabaseNames.Add("mariadb_climsoft_db_v4", Chr(34) & "mariadb_climsoft_db_v4" & Chr(34))
        dctDatabaseNames.Add("mariadb_climsoft_test_db_v4", Chr(34) & "mariadb_climsoft_test_db_v4" & Chr(34))
        ucrComboBoxDatabaseName.SetParameter(New RParameter("dbname", 0))
        ucrComboBoxDatabaseName.SetItems(dctDatabaseNames)
        ucrComboBoxDatabaseName.bAllowNonConditionValues = True

        'host e.g 127.0.0.1
        ucrTxtHost.SetParameter(New RParameter("host", 1))

        'ports
        dctPorts.Add("3308", "3308")
        dctPorts.Add("3306", "3306")
        ucrComboBoxPort.SetParameter(New RParameter("port", 2))
        ucrComboBoxPort.SetItems(dctPorts)
        ucrComboBoxPort.AddQuotesIfUnrecognised = False
        ucrComboBoxPort.bAllowNonConditionValues = True

        'user name
        ucrTxtUserName.SetParameter(New RParameter("user", 3))

    End Sub

    Private Sub SetDefaults()
        clsRDatabaseConnect = New RFunction
        clsRDatabaseDisconnect = New RFunction
        clsRHasConnection = New RFunction

        'set database connect R command and it's parameter values(from previous set values if available)
        clsRDatabaseConnect.SetRCommand(frmMain.clsRLink.strInstatDataObject & "$database_connect")
        clsRDatabaseConnect.AddParameter("dbname", frmMain.clsInstatOptions.strClimsoftDatabaseName, iPosition:=0)
        clsRDatabaseConnect.AddParameter("host", frmMain.clsInstatOptions.strClimsoftHost, iPosition:=1)
        clsRDatabaseConnect.AddParameter("port", frmMain.clsInstatOptions.strClimsoftPort, iPosition:=2)
        clsRDatabaseConnect.AddParameter("user", frmMain.clsInstatOptions.strClimsoftUsername, iPosition:=3)

        'set database disconnect R command
        clsRDatabaseDisconnect.SetRCommand(frmMain.clsRLink.strInstatDataObject & "$database_disconnect")

        'set has connection R command
        clsRHasConnection.SetRCommand(frmMain.clsRLink.strInstatDataObject & "$has_database_connection")

        'disconnect if was already connected 
        If HasConnection() Then
            Disconnect()
        End If
        bConnected = False
        UpdateConnectionState()
    End Sub

    Private Sub SetRCodeForControls()
        SetRCode(Me, clsRDatabaseConnect, False)
    End Sub

    Public Function ConnectionActive() As Boolean
        Return bConnected
    End Function

    Private Sub UpdateConnectionState()
        If bConnected Then
            lblConnection.Text = "Connected"
            lblConnection.ForeColor = Color.Green
            btnConnect.Text = "Disconnect"

            'disable all other controls. Entry allowed only when there is no existing connection
            ucrComboBoxDatabaseName.Enabled = False
            ucrComboBoxPort.Enabled = False
            ucrTxtHost.Enabled = False
            ucrTxtUserName.Enabled = False
            chkRememberCredentials.Enabled = False
        Else
            lblConnection.Text = "No Connection"
            lblConnection.ForeColor = Color.Red
            btnConnect.Text = "Connect"

            'enable all other controls to allow entry of connection details
            ucrComboBoxDatabaseName.Enabled = True
            ucrComboBoxPort.Enabled = True
            ucrTxtHost.Enabled = True
            ucrTxtUserName.Enabled = True
            chkRememberCredentials.Enabled = True
        End If
    End Sub

    Private Sub Disconnect()
        frmMain.clsRLink.RunScript(clsRDatabaseDisconnect.ToScript(), strComment:="Disconnect database connection.", bSeparateThread:=False, bShowWaitDialogOverride:=False)
    End Sub

    Private Function HasConnection() As Boolean
        Dim expTemp As SymbolicExpression
        expTemp = frmMain.clsRLink.RunInternalScriptGetValue(clsRHasConnection.ToScript())
        Return (Not expTemp.Type = Internals.SymbolicExpressionType.Null) AndAlso expTemp.AsLogical(0)
    End Function

    Private Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        btnConnect.Enabled = False 'temporary disable 

        'if was already connected, then user action is meant to disconnect else, try connecting to database
        If bConnected Then
            Disconnect()
            bConnected = False
        Else
            'will display an R password input prompt, to enter password and attempt connecting to database
            frmMain.clsRLink.RunScript(clsRDatabaseConnect.ToScript(), strComment:="Connect database connection.", bSeparateThread:=False, bShowWaitDialogOverride:=False)
            bConnected = HasConnection()
        End If
        UpdateConnectionState()
        btnConnect.Enabled = True

        If chkRememberCredentials.Checked AndAlso bConnected Then
            'save credentials before
            frmMain.clsInstatOptions.SetClimsoftDatabaseName(ucrComboBoxDatabaseName.GetText())
            frmMain.clsInstatOptions.SetClimsoftHost(ucrTxtHost.GetText())
            frmMain.clsInstatOptions.SetClimsoftPort(ucrComboBoxPort.GetText())
            frmMain.clsInstatOptions.SetClimsoftUsername(ucrTxtUserName.GetText())
        End If

    End Sub

    Private Sub ucrControlsContents_ControlContentsChanged(ucrChangedControl As ucrCore) Handles ucrComboBoxDatabaseName.ControlContentsChanged, ucrTxtHost.ControlContentsChanged, ucrComboBoxPort.ControlContentsChanged, ucrTxtUserName.ControlContentsChanged
        'if not already connected, check if it's valid to attempt connecting to database
        If bConnected Then
            Exit Sub
        End If
        btnConnect.Enabled = Not ucrTxtHost.IsEmpty AndAlso Not ucrTxtUserName.IsEmpty AndAlso Not ucrComboBoxPort.IsEmpty AndAlso Not ucrComboBoxDatabaseName.IsEmpty
    End Sub

End Class