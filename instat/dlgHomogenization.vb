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

Public Class dlgHomogenization
    Private bFirstLoad As Boolean = True
    Private bReset As Boolean = True
    Private clsCptMeanFunction, clsCptVarianceFunction, clsCptMeanVarianceFunction, clsExcludeNAFunction, clsPlotFunction, clsSummaryFunction As New RFunction
    Private Sub dlgHomogenization_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        autoTranslate(Me)
        If bFirstLoad Then
            InitialiseDialog()
            bFirstLoad = False
        End If

        If bReset Then
            SetDefaults()
        End If
        SetRcodeForControls(bReset)
        bReset = False
        TestOkEnabled()
    End Sub

    Private Sub InitialiseDialog()
        Dim dctPenaltyOptions As New Dictionary(Of String, String)
        Dim dctMethodOptions As New Dictionary(Of String, String)
        Dim dctDistributionOptions As New Dictionary(Of String, String)

        ucrReceiverElement.Selector = ucrSelectorHomogenization
        ucrBase.clsRsyntax.iCallType = 2

        ucrReceiverElement.SetParameter(New RParameter("object", 0))
        ucrReceiverElement.SetMeAsReceiver()
        ucrReceiverElement.SetParameterIsRFunction()

        ucrPnlMethods.AddRadioButton(rdoCptMean)
        ucrPnlMethods.AddRadioButton(rdoCptVariance)
        ucrPnlMethods.AddRadioButton(rdoMeanVariance)
        ucrPnlMethods.AddFunctionNamesCondition(rdoCptMean, "cpt.mean")
        ucrPnlMethods.AddFunctionNamesCondition(rdoCptVariance, "cpt.var")
        ucrPnlMethods.AddFunctionNamesCondition(rdoMeanVariance, "cpt.meanvar")

        ucrPnlOptions.AddRadioButton(rdoSingle)
        ucrPnlOptions.AddRadioButton(rdoNeighbouring)
        ucrPnlOptions.AddRadioButton(rdoMultiple)
        ucrPnlOptions.AddFunctionNamesCondition(rdoSingle, {"cpt.mean", "cpt.var", "cpt.meanvar"})
        ucrPnlOptions.AddFunctionNamesCondition(rdoNeighbouring, {"cpt.mean", "cpt.var", "cpt.meanvar"}, False)
        ucrPnlOptions.AddFunctionNamesCondition(rdoMultiple, {"cpt.mean", "cpt.var", "cpt.meanvar"}, False)

        ucrChkPlot.SetText("Plot")
        ucrChkPlot.AddRSyntaxContainsFunctionNamesCondition(True, {"plot"})
        ucrChkPlot.AddRSyntaxContainsFunctionNamesCondition(False, {"plot"}, False)

        ucrChkSummary.SetText("Summary")
        ucrChkSummary.AddRSyntaxContainsFunctionNamesCondition(True, {"summary"})
        ucrChkSummary.AddRSyntaxContainsFunctionNamesCondition(False, {"summary"}, False)

        ucrInputComboPenalty.SetParameter(New RParameter("penalty", 1))
        dctPenaltyOptions.Add("None", Chr(34) & "None" & Chr(34))
        dctPenaltyOptions.Add("SIC", Chr(34) & "SIC" & Chr(34))
        dctPenaltyOptions.Add("BIC", Chr(34) & "BIC" & Chr(34))
        dctPenaltyOptions.Add("MBIC", Chr(34) & "MBIC" & Chr(34))
        dctPenaltyOptions.Add("AIC", Chr(34) & "AIC" & Chr(34))
        dctPenaltyOptions.Add("Hannan-Quinn", Chr(34) & "Hannan-Quinn" & Chr(34))
        dctPenaltyOptions.Add("Asymptotic", Chr(34) & "Asymptotic" & Chr(34))
        dctPenaltyOptions.Add("Manual", Chr(34) & "Manual" & Chr(34))
        dctPenaltyOptions.Add("CROPS", Chr(34) & "CROPS" & Chr(34))
        ucrInputComboPenalty.SetItems(dctPenaltyOptions)
        ucrInputComboPenalty.SetDropDownStyleAsNonEditable()
        ucrInputComboPenalty.SetRDefault(Chr(34) & "None" & Chr(34))

        ucrInputComboMethod.SetParameter(New RParameter("method", 2))
        dctMethodOptions.Add("AMOC", Chr(34) & "AMOC" & Chr(34))
        dctMethodOptions.Add("PELT", Chr(34) & "PELT" & Chr(34))
        dctMethodOptions.Add("SegNeigh", Chr(34) & "SegNeigh" & Chr(34))
        dctMethodOptions.Add("BinSeg", Chr(34) & "BinSeg" & Chr(34))
        ucrInputComboMethod.SetItems(dctMethodOptions)
        ucrInputComboMethod.SetDropDownStyleAsNonEditable()

        ucrInputComboDistribution.SetParameter(New RParameter("test.stat", 3))
        dctDistributionOptions.Add("Normal", Chr(34) & "Normal" & Chr(34))
        dctDistributionOptions.Add("CUSUM", Chr(34) & "CUSUM" & Chr(34))
        ucrInputComboDistribution.SetItems(dctDistributionOptions)
        ucrInputComboDistribution.SetDropDownStyleAsNonEditable()
        ucrInputComboDistribution.SetRDefault(Chr(34) & "Normal" & Chr(34))

        ucrNudMinSegLen.SetParameter(New RParameter("minseglen", 4))
        ucrNudMinSegLen.SetRDefault(1)
        ttOptions.SetToolTip(ucrNudMinSegLen.nudUpDown, "Positive integer giving the minimum segment length (no. of observations between changes), default is the minimum allowed by theory.")

        ucrInputQ.SetParameter(New RParameter("Q", 5))
        ucrInputQ.AddQuotesIfUnrecognised = False
        ucrInputQ.SetValidationTypeAsNumeric()
        ttOptions.SetToolTip(ucrInputQ.txtInput, "The maximum number of changepoints to search for using the BinSeg method")

        ucrInputPenValue.SetParameter(New RParameter("pen.value", 6))
        ucrInputPenValue.AddQuotesIfUnrecognised = False
        ucrInputPenValue.SetValidationTypeAsNumeric()
        ttOptions.SetToolTip(ucrInputPenValue.txtInput, "The theoretical type I error e.g.0.05 when using the Asymptotic penalty. A vector of length 2 (min,max) if using the CROPS penalty")

        ucrSaveResult.SetLabelText("Save Result:")
        ucrSaveResult.SetDataFrameSelector(ucrSelectorHomogenization.ucrAvailableDataFrames)
        ucrSaveResult.SetSaveTypeAsModel()
        ucrSaveResult.SetIsComboBox()
        ucrSaveResult.SetPrefix("Result")
        ucrSaveResult.SetAssignToIfUncheckedValue("last_result")

        ucrInputComboPenalty.AddToLinkedControls(ucrInputPenValue, {"Asymptotic", "CROPS"}, bNewLinkedAddRemoveParameter:=True, bNewLinkedHideIfParameterMissing:=True)
        ucrInputComboMethod.AddToLinkedControls(ucrInputQ, {"BinSeg"}, bNewLinkedAddRemoveParameter:=True, bNewLinkedHideIfParameterMissing:=True)
        ucrPnlOptions.AddToLinkedControls(ucrReceiverNeighbour, {rdoNeighbouring}, bNewLinkedAddRemoveParameter:=True, bNewLinkedHideIfParameterMissing:=True)
        ucrReceiverNeighbour.SetLinkedDisplayControl(lblNeighbouring)
        ucrInputPenValue.SetLinkedDisplayControl(lblPenaltyValue)
        ucrInputQ.SetLinkedDisplayControl(lblQ)

        'Not yet working!
        rdoMultiple.Enabled = False
        rdoNeighbouring.Enabled = False
    End Sub

    Private Sub SetDefaults()
        clsCptMeanFunction = New RFunction
        clsCptVarianceFunction = New RFunction
        clsCptMeanVarianceFunction = New RFunction
        clsExcludeNAFunction = New RFunction
        clsPlotFunction = New RFunction
        clsSummaryFunction = New RFunction

        ucrSelectorHomogenization.Reset()
        ucrSaveResult.Reset()
        ucrBase.clsRsyntax.bExcludeAssignedFunctionOutput = False

        clsCptMeanFunction.SetPackageName("changepoint")
        clsCptMeanFunction.SetRCommand("cpt.mean")
        clsCptMeanFunction.AddParameter("data", clsRFunctionParameter:=clsExcludeNAFunction, iPosition:=0)
        clsCptMeanFunction.AddParameter("Q", 5, iPosition:=5)
        clsCptMeanFunction.AddParameter("pen.value", 0, iPosition:=6)
        clsCptMeanFunction.AddParameter("method", Chr(34) & "BinSeg" & Chr(34), iPosition:=2)

        clsCptVarianceFunction.SetPackageName("changepoint")
        clsCptVarianceFunction.SetRCommand("cpt.var")
        clsCptVarianceFunction.AddParameter("data", clsRFunctionParameter:=clsExcludeNAFunction, iPosition:=0)

        clsCptMeanVarianceFunction.SetPackageName("changepoint")
        clsCptMeanVarianceFunction.SetRCommand("cpt.meanvar")
        clsCptMeanVarianceFunction.AddParameter("data", clsRFunctionParameter:=clsExcludeNAFunction, iPosition:=0)

        clsExcludeNAFunction.SetRCommand("na.exclude")

        clsPlotFunction.SetRCommand("plot")

        clsSummaryFunction.SetRCommand("summary")

        ucrBase.clsRsyntax.ClearCodes()
        AddPlotSummaryParameters()
        ucrBase.clsRsyntax.SetBaseRFunction(clsCptMeanFunction)
    End Sub

    Private Sub SetRcodeForControls(bReset As Boolean)
        ucrReceiverElement.SetRCode(clsExcludeNAFunction, bReset)

        ucrInputComboPenalty.AddAdditionalCodeParameterPair(clsCptVarianceFunction, ucrInputComboPenalty.GetParameter, iAdditionalPairNo:=1)
        ucrInputComboPenalty.AddAdditionalCodeParameterPair(clsCptMeanVarianceFunction, ucrInputComboPenalty.GetParameter, iAdditionalPairNo:=1)
        ucrInputComboPenalty.SetRCode(clsCptMeanFunction, bReset)

        ucrInputComboMethod.AddAdditionalCodeParameterPair(clsCptVarianceFunction, ucrInputComboMethod.GetParameter, iAdditionalPairNo:=1)
        ucrInputComboMethod.AddAdditionalCodeParameterPair(clsCptMeanVarianceFunction, ucrInputComboMethod.GetParameter, iAdditionalPairNo:=2)
        ucrInputComboMethod.SetRCode(clsCptMeanFunction, bReset)

        ucrInputComboDistribution.AddAdditionalCodeParameterPair(clsCptVarianceFunction, ucrInputComboDistribution.GetParameter, iAdditionalPairNo:=1)
        ucrInputComboDistribution.AddAdditionalCodeParameterPair(clsCptMeanVarianceFunction, ucrInputComboDistribution.GetParameter, iAdditionalPairNo:=2)
        ucrInputComboDistribution.SetRCode(clsCptMeanFunction, bReset)

        ucrNudMinSegLen.AddAdditionalCodeParameterPair(clsCptVarianceFunction, ucrNudMinSegLen.GetParameter, iAdditionalPairNo:=1)
        ucrNudMinSegLen.AddAdditionalCodeParameterPair(clsCptMeanVarianceFunction, ucrNudMinSegLen.GetParameter, iAdditionalPairNo:=2)
        ucrNudMinSegLen.SetRCode(clsCptMeanFunction, bReset)

        ucrInputQ.AddAdditionalCodeParameterPair(clsCptVarianceFunction, ucrInputQ.GetParameter, iAdditionalPairNo:=1)
        ucrInputQ.AddAdditionalCodeParameterPair(clsCptMeanVarianceFunction, ucrInputQ.GetParameter, iAdditionalPairNo:=2)
        ucrInputQ.SetRCode(clsCptMeanFunction, bReset)

        ucrInputPenValue.AddAdditionalCodeParameterPair(clsCptVarianceFunction, ucrInputPenValue.GetParameter, iAdditionalPairNo:=1)
        ucrInputPenValue.AddAdditionalCodeParameterPair(clsCptMeanVarianceFunction, ucrInputPenValue.GetParameter, iAdditionalPairNo:=2)
        ucrInputPenValue.SetRCode(clsCptMeanFunction, bReset)

        ucrPnlMethods.SetRCode(ucrBase.clsRsyntax.clsBaseFunction, bReset)
        ucrPnlOptions.SetRCode(ucrBase.clsRsyntax.clsBaseFunction, bReset)

        ucrSaveResult.AddAdditionalRCode(clsCptVarianceFunction, iAdditionalPairNo:=1)
        ucrSaveResult.AddAdditionalRCode(clsCptMeanVarianceFunction, iAdditionalPairNo:=2)
        ucrSaveResult.SetRCode(clsCptMeanFunction, bReset)

        ucrChkPlot.SetRSyntax(ucrBase.clsRsyntax, bReset)
        ucrChkSummary.SetRSyntax(ucrBase.clsRsyntax, bReset)
    End Sub

    Private Sub TestOkEnabled()
        If ucrReceiverElement.IsEmpty OrElse Not ucrSaveResult.IsComplete OrElse (ucrInputComboMethod.GetText = "BinSeg" AndAlso ucrInputQ.IsEmpty) OrElse (ucrInputComboPenalty.GetText = "Asymptotic" AndAlso ucrInputPenValue.IsEmpty) OrElse ucrNudMinSegLen.GetText = "" Then
            ucrBase.OKEnabled(False)
        Else
            ucrBase.OKEnabled(True)
        End If
    End Sub

    Private Sub ucrPnlMethods_ControlValueChanged(ucrChangedControl As ucrCore) Handles ucrPnlMethods.ControlValueChanged
        If rdoCptMean.Checked Then
            ucrBase.clsRsyntax.SetBaseRFunction(clsCptMeanFunction)
        ElseIf rdoCptVariance.Checked Then
            ucrBase.clsRsyntax.SetBaseRFunction(clsCptVarianceFunction)
        ElseIf rdoMeanVariance.Checked Then
            ucrBase.clsRsyntax.SetBaseRFunction(clsCptMeanVarianceFunction)
        End If
        AddPlotSummaryParameters()
    End Sub

    Private Sub AddPlotSummaryParameters()
        If rdoCptMean.Checked Then
            clsPlotFunction.AddParameter("x", clsRFunctionParameter:=clsCptMeanFunction, iPosition:=0)
            clsSummaryFunction.AddParameter("object", clsRFunctionParameter:=clsCptMeanFunction, iPosition:=0)
        ElseIf rdoCptVariance.Checked Then
            clsPlotFunction.AddParameter("x", clsRFunctionParameter:=clsCptVarianceFunction, iPosition:=0)
            clsSummaryFunction.AddParameter("object", clsRFunctionParameter:=clsCptVarianceFunction, iPosition:=0)
        ElseIf rdoMeanVariance.Checked Then
            clsPlotFunction.AddParameter("x", clsRFunctionParameter:=clsCptMeanVarianceFunction, iPosition:=0)
            clsSummaryFunction.AddParameter("object", clsRFunctionParameter:=clsCptMeanVarianceFunction, iPosition:=0)
        End If
    End Sub

    Private Sub ucrChkSummary_ControlValueChanged(ucrChangedControl As ucrCore) Handles ucrChkSummary.ControlValueChanged
        If ucrChkSummary.Checked Then
            ucrBase.clsRsyntax.AddToAfterCodes(clsSummaryFunction, iPosition:=0)
            clsSummaryFunction.iCallType = 2
        Else
            ucrBase.clsRsyntax.RemoveFromAfterCodes(clsSummaryFunction)
        End If
    End Sub

    Private Sub ucrChkPlot_ControlValueChanged(ucrChangedControl As ucrCore) Handles ucrChkPlot.ControlValueChanged
        If ucrChkPlot.Checked Then
            ucrBase.clsRsyntax.AddToAfterCodes(clsPlotFunction, iPosition:=1)
            clsPlotFunction.iCallType = 3
        Else
            ucrBase.clsRsyntax.RemoveFromAfterCodes(clsPlotFunction)
        End If
    End Sub
    Private Sub ucrBase_ClickReset(sender As Object, e As EventArgs) Handles ucrBase.ClickReset
        SetDefaults()
        SetRcodeForControls(True)
        TestOkEnabled()
    End Sub

    Private Sub Controls_ControlContentsChanged(ucrChangedControl As ucrCore) Handles ucrReceiverElement.ControlContentsChanged, ucrSaveResult.ControlContentsChanged, ucrInputQ.ControlContentsChanged, ucrInputPenValue.ControlContentsChanged, ucrNudMinSegLen.ControlContentsChanged, ucrInputComboMethod.ControlContentsChanged, ucrInputComboPenalty.ControlContentsChanged
        TestOkEnabled()
    End Sub
End Class