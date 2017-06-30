Imports System.IO
Imports System.Runtime.InteropServices
Imports LCARS.x32

public Class modBusiness

#Region " Structures "

    Private Class ExternalApp
        Public hWnd As Integer
        Public MainWindowText As String
    End Class

    Public Structure UserButtonInfo
        Dim color As String
        Dim Name As String
        Dim Location As String
    End Structure
#End Region

#Region " Global Variables "

#Region " Public Global Variables "

    'Common form components
    Public myForm As Form
    Public myMainBar As Panel
    Public myMainPanel As Panel
    Public ProgramsPanel As LCARS.Controls.WindowlessContainer
    Public UserButtonsPanel As LCARS.Controls.ButtonGrid
    Public myAppsPanel As Panel

    'Common Buttons
    Public myStartMenu As LCARS.LCARSbuttonClass
    Public myComputer As LCARS.LCARSbuttonClass
    Public mySettings As LCARS.LCARSbuttonClass
    Public myEngineering As LCARS.LCARSbuttonClass
    Public myModeSelect As LCARS.LCARSbuttonClass
    Public myDeactivate As LCARS.LCARSbuttonClass
    Public myAlert As LCARS.LCARSbuttonClass
    Public myDestruct As LCARS.LCARSbuttonClass
    Public myClock As Control
    Public myPhoto As LCARS.LCARSbuttonClass
    Public myWebBrowser As LCARS.LCARSbuttonClass
    Public myButtonManager As LCARS.LCARSbuttonClass
    Public myUserButtons As LCARS.LCARSbuttonClass
    Public myDocuments As LCARS.LCARSbuttonClass
    Public myPictures As LCARS.LCARSbuttonClass
    Public myVideos As LCARS.LCARSbuttonClass
    Public myMusic As LCARS.LCARSbuttonClass
    Public myBattery As Panel
    Public myTrayPanel As Panel
    Public myShowTrayButton As LCARS.LCARSbuttonClass
    Public myHideTrayButton As LCARS.Controls.ArrowButton
    Public myOSK As LCARS.Controls.FlatButton
    Public mySpeech As LCARS.Controls.FlatButton
    Public myHelp As LCARS.LCARSbuttonClass
    Public myRun As LCARS.LCARSbuttonClass
    Public myAlertListButton As LCARS.LCARSbuttonClass
    Public myProgramPagesDisplay As LCARS.LCARSbuttonClass
    Dim bars() As LCARS.LCARSbuttonClass
    Dim myBattPercent As Control
    Dim myPowerSource As Control
    Public myProgsUp As LCARS.LCARSbuttonClass
    Public myProgsBack As LCARS.LCARSbuttonClass
    Public myProgsNext As LCARS.LCARSbuttonClass

    Public MyPrograms As DirectoryStartItem
    Public myUserButtonCollection As New List(Of UserButtonInfo)
    Public mainTimer As New Timer
    Public WithEvents tmrAutohide As New Timer()
    Public ScreenIndex As Integer

    Public leftArrow As LCARS.Controls.ArrowButton
    Public rightArrow As LCARS.Controls.ArrowButton

    Public progShowing As Boolean
    Public userButtonsShowing As Boolean

#End Region

#Region " Private Global Variables "

    'Program Pages
    Dim ProgDir As New List(Of Integer)
    Dim ProgPageSize As Integer
    Dim curProgPage As Integer = 1
    Dim pageCount As Integer
    Dim curProgIndex As Integer

    'External application management
    Dim myWindows As New List(Of ExternalApp) 'Current list of windows
    Dim WindowList As New List(Of ExternalApp) 'Used by fEnumWindows
    Dim curTop As Integer

    'Time Format
    Dim timeFormat As String = "h:mm:sstt"
    Dim dateFormat As String = "M/d/yyyy"

    'On Screen Keyboard (OSK)
    Dim OSKproc As New Process
    Dim isVisible As Boolean = False

    'Autohide
    Dim autohide As IAutohide.AutoHideModes
    Dim hideCount As Integer = 0

    Private oldArea As Rectangle

#End Region

#End Region

    Private Function fEnumWindowsCallBack(ByVal hwnd As Integer, ByVal lParam As Integer) As Integer
        'Abort if we're closing/closed
        If myForm.IsDisposed Then Return False
        'Invisible windows should not be shown
        If Not IsWindowVisible(hwnd) Then Return True
        'Windows with a parent should not be shown
        If GetParent(hwnd) <> 0 Then Return True

        Dim bNoOwner As Integer = (GetWindow(hwnd, GW_OWNER) = 0)
        Dim lExStyle As Integer = GetWindowLong_Safe(hwnd, GWL_EXSTYLE)

        'This if statement is from code found at http://msdntracker.blogspot.com/2008/03/list-currently-opened-windows-with.html
        If ((((lExStyle And WS_EX_TOOLWINDOW) = 0) And bNoOwner) Or _
            ((lExStyle And WS_EX_APPWINDOW) And Not bNoOwner)) _
            And ((lExStyle And WS_EX_NOREDIRECTIONBITMAP) = 0) Then

            'Check to see if it's actually on this screen
            Dim screen1, screen2 As Integer
            screen1 = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST)
            screen2 = MonitorFromWindow(myForm.Handle, MONITOR_DEFAULTTONEAREST)
            If screen1 <> screen2 Then Return True

            'Check to see if it's on the current virtual desktop
            Try
                If Not VirtualDesktops.IsWindowOnCurrentVirtualDesktop(hwnd) Then Return True
            Catch ex As COMException
            End Try

            ' Get the window's caption.
            Dim sWindowText As String = Space(256)
            Dim lReturn As Integer = GetWindowText(hwnd, sWindowText, Len(sWindowText))
            If lReturn <> 0 Then
                ' Add it to our list.
                sWindowText = Left(sWindowText, lReturn)

                Dim myApp As New ExternalApp() With {.hWnd = hwnd, .MainWindowText = Trim(sWindowText)}
                WindowList.Add(myApp)
            End If
        End If
        Return True
    End Function

    Public Sub myStartMenu_Click(ByVal sender As Object, ByVal e As System.EventArgs)

    End Sub

    Public Sub myCompButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub mySettingsButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim mySettings As New frmSettings()
        MoveToScreen(mySettings.Handle)
        mySettings.Show()
    End Sub

    Public Sub myEngineeringButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSengineering.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myModeSelectButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        'Check that the images directory exists. If not, create it.
        If Not Directory.Exists(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\LCARS x32\Images") Then
            Directory.CreateDirectory(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\LCARS x32\Images")
        End If
        'Save screenshot and show the selection form
        Dim screenImage As New Bitmap(myForm.Width, myForm.Height)
        Dim g As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(screenImage)
        g.CopyFromScreen(myForm.PointToScreen(New Point(0, 0)), New Point(0, 0), myForm.Size)
        Try
            screenImage.Save(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "\LCARS x32\Images\" & myForm.Name.ToLower() & "_" & ScreenIndex & ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg)
        Catch ex As Exception
            MsgBox("Error saving image for interface " & myForm.Name & " on screen " & ScreenIndex & ".")
        End Try
        SetParent(hTrayIcons, hTrayParent)
        Dim myChoice As New ScreenChooserDialog(ScreenIndex)
        MoveToScreen(myChoice.Handle)
        myChoice.Show()
    End Sub

    Public Sub myDeactivateButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSshutdown.exe"
        myProcess.StartInfo.Arguments = "/" & myDesktop.Handle.ToString()
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myAlertButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If AlertActive Then
            CancelAlert()
        Else
            GeneralAlert(0)
        End If
    End Sub

    Public Sub myYellowAlertButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If AlertActive Then
            CancelAlert()
        Else
            GeneralAlert(1)
        End If
    End Sub

    Public Sub myDestructButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSdestruct.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myPhoto_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSpic.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myWebBrowser_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSWebBrowser.exe"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myButtonManager_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myUserButtons As New frmManageButtons(Me)

        myUserButtons.Show()
    End Sub

    Public Sub myUserButtons_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        If UserButtonsPanel.Visible = True Then
            UserButtonsPanel.Visible = False
            myButtonManager.Visible = False
        Else
            UserButtonsPanel.Visible = True
            myButtonManager.Visible = True
        End If
        progShowing = ProgramsPanel.Visible
        userButtonsShowing = UserButtonsPanel.Visible
        myMainBar.Width -= 1
        myMainBar.Width += 1


    End Sub

    Public Sub myDocuments_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        myProcess.StartInfo.Arguments = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myPictures_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        myProcess.StartInfo.Arguments = System.Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        launchProcessOnScreen(myProcess)
    End Sub
    Public Sub myVideos_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        myProcess.StartInfo.Arguments = GetMyVideosPath()
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myMusic_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\LCARSexplorer.exe"
        myProcess.StartInfo.Arguments = System.Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myShowTrayButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        modSettings.ShowTrayIcons(ScreenIndex) = True
        Dim myPlacement As New WINDOWPLACEMENT
        GetWindowPlacement(hTrayIcons, myPlacement)
        Dim myWidth As Integer = myPlacement.rcNormalPosition.Right_Renamed - myPlacement.rcNormalPosition.Left_Renamed

        myAppsPanel.Width -= (myWidth + myHideTrayButton.Width) - myTrayPanel.Width
        myTrayPanel.Left -= (myWidth + myHideTrayButton.Width) - myTrayPanel.Width

        myTrayPanel.Width = myWidth + myHideTrayButton.Width

        myShowTrayButton.Visible = False
        myHideTrayButton.Visible = True
        SetParent(hTrayIcons, myTrayPanel.Handle)

    End Sub

    Public Sub myHideTrayButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        modSettings.ShowTrayIcons(ScreenIndex) = False
        Dim mywidth As Integer = myShowTrayButton.Width - myTrayPanel.Width
        myTrayPanel.Width = myShowTrayButton.Width
        myTrayPanel.Left -= mywidth
        myAppsPanel.Width -= mywidth
        myHideTrayButton.Visible = False
        myShowTrayButton.Visible = True
        myShowTrayButton.BringToFront()
        SetParent(hTrayIcons, myIconSaver.Handle)
    End Sub

    Public Sub myOSK_Click(ByVal Sender As Object, ByVal e As System.EventArgs)
        If OSKproc.StartInfo.FileName = "" Then
            OSKproc = Process.Start(Application.StartupPath & "\OnScreenKeyboard.exe")
            isVisible = True
        ElseIf OSKproc.StartInfo.FileName <> "" And OSKproc.HasExited = True Then
            OSKproc = Process.Start(Application.StartupPath & "\OnScreenKeyboard.exe")
            isVisible = True
        Else
            If isVisible Then
                ShowWindow(OSKproc.MainWindowHandle, SW_HIDE)
                isVisible = False
            Else
                ShowWindow(OSKproc.MainWindowHandle, SW_SHOW)
                isVisible = True
            End If
        End If
    End Sub

    Public Sub mySpeech_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If console.Visible Then
            If MonitorFromWindow(myForm.Handle, MONITOR_DEFAULTTONEAREST) = MonitorFromWindow(console.Handle, MONITOR_DEFAULTTONEAREST) Then
                console.Hide()
            Else
                MoveToScreen(console.Handle)
            End If
        Else
            modSpeech.ShowConsole()
            MoveToScreen(console.Handle)
        End If
    End Sub

    Public Sub myHelp_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myProcess As New Process()
        myProcess.StartInfo.FileName = Application.StartupPath & "\Lcarsx32 Manual.exe"
        myProcess.StartInfo.Arguments = Application.StartupPath & "\LCARS x32 Manual"
        launchProcessOnScreen(myProcess)
    End Sub

    Public Sub myRun_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myRunDialog As New frmRunProgram
        MoveToScreen(myRunDialog.Handle)
        myRunDialog.Show()
    End Sub

    Public Sub myAlertListButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        If frmAlerts.Visible Then
            frmAlerts.Hide()
        Else
            frmAlerts.Show()
        End If
    End Sub

    Public Sub init(ByRef curForm As Form)
        'When a mainscreen loads, it calls this sub to let LCARS x32 know
        'that it is now the mainscreen.  Since most of the functions of the
        'mainscreen are done through this module, it is imperitive that they
        'call this sub as soon as they load.
        setBusiness(Me, ScreenIndex)

        myForm = curForm

        'Set the form's extended style to "WS_EX_TOOLWINDOW" which allows it
        'to stay fullscreen instead of being resized by the working area.
        Dim currentStyle As Integer = GetWindowLong_Safe(myForm.Handle, GWL_EXSTYLE)
        currentStyle = currentStyle Or (WS_EX_TOOLWINDOW)
        SetWindowLong_Safe(myForm.Handle, GWL_EXSTYLE, currentStyle)

        'Set the various panels and buttons that are controlled by this module.
        'These panels and buttons behave exactly the same on each mainscreen.
        ProgramsPanel = myForm.Controls.Find("pnlPrograms", True)(0)
        'ProgramsButton = myForm.Controls.Find("fbPrograms", True)(0)
        myMainPanel = myForm.Controls.Find("pnlMain", True)(0)
        myMainBar = myForm.Controls.Find("pnlMainBar", True)(0)
        UserButtonsPanel = myForm.Controls.Find("gridUserButtons", True)(0)
        myAppsPanel = myForm.Controls.Find("pnlApps", True)(0)

        'Mainscreen Buttons:
        myStartMenu = myForm.Controls.Find("myStartMenu", True)(0)
        myComputer = myForm.Controls.Find("MyComp", True)(0)
        mySettings = myForm.Controls.Find("mySettings", True)(0)
        myEngineering = myForm.Controls.Find("myEngineering", True)(0)
        myModeSelect = myForm.Controls.Find("myModeSelect", True)(0)
        myDeactivate = myForm.Controls.Find("myDeactivate", True)(0)
        myAlert = myForm.Controls.Find("myAlert", True)(0)
        'myYellowAlert = myForm.Controls.Find("myYellowAlert", True)(0)
        myDestruct = myForm.Controls.Find("myDestruct", True)(0)
        myClock = myForm.Controls.Find("myClock", True)(0)
        myPhoto = myForm.Controls.Find("myPhoto", True)(0)
        myWebBrowser = myForm.Controls.Find("fbWebBrowser", True)(0)
        myButtonManager = myForm.Controls.Find("myButtonManager", True)(0)
        myUserButtons = myForm.Controls.Find("myUserButtons", True)(0)
        myDocuments = myForm.Controls.Find("myDocuments", True)(0)
        myPictures = myForm.Controls.Find("myPictures", True)(0)
        myVideos = myForm.Controls.Find("myVideos", True)(0)
        myMusic = myForm.Controls.Find("myMusic", True)(0)
        myBattery = myForm.Controls.Find("pnlBatt", True)(0)
        myTrayPanel = myForm.Controls.Find("pnlTray", True)(0)
        myShowTrayButton = myForm.Controls.Find("ShowTrayButton", True)(0)
        myHideTrayButton = myForm.Controls.Find("HideTrayButton", True)(0)
        myOSK = myForm.Controls.Find("myOSK", True)(0)
        mySpeech = myForm.Controls.Find("mySpeech", True)(0)
        myHelp = myForm.Controls.Find("myHelp", True)(0)
        myRun = myForm.Controls.Find("myRun", True)(0)
        myAlertListButton = myForm.Controls.Find("myAlertListButton", True)(0)
        myProgramPagesDisplay = myForm.Controls.Find("fbProgramPages", True)(0)
        bars = New LCARS.LCARSbuttonClass(9) { _
            myBattery.Controls("fbBatt1"), _
            myBattery.Controls("fbBatt2"), _
            myBattery.Controls("fbBatt3"), _
            myBattery.Controls("fbBatt4"), _
            myBattery.Controls("fbBatt5"), _
            myBattery.Controls("fbBatt6"), _
            myBattery.Controls("fbBatt7"), _
            myBattery.Controls("fbBatt8"), _
            myBattery.Controls("fbBatt9"), _
            myBattery.Controls("fbBatt10")}
        myBattPercent = myBattery.Controls("lblBatt")
        myPowerSource = myBattery.Controls("lblPowerSource")
        myProgsUp = myForm.Controls.Find("myProgsUp", True)(0)
        myProgsBack = myForm.Controls.Find("myProgsBack", True)(0)
        myProgsNext = myForm.Controls.Find("myProgsNext", True)(0)

        mySpeech.Lit = modSpeech.SpeechEnabled
        'event handlers:
        AddHandler ProgramsPanel.Resize, AddressOf ProgramsPanel_Resize
        AddHandler myStartMenu.Click, AddressOf myStartMenu_Click
        AddHandler myComputer.Click, AddressOf myCompButton_Click
        AddHandler mySettings.Click, AddressOf mySettingsButton_Click
        AddHandler myEngineering.Click, AddressOf myEngineeringButton_Click
        AddHandler myModeSelect.Click, AddressOf myModeSelectButton_Click
        AddHandler myDeactivate.Click, AddressOf myDeactivateButton_Click
        AddHandler myAlert.Click, AddressOf myAlertButton_Click
        AddHandler myDestruct.Click, AddressOf myDestructButton_Click
        AddHandler myPhoto.Click, AddressOf myPhoto_Click
        AddHandler myWebBrowser.Click, AddressOf myWebBrowser_Click
        AddHandler myButtonManager.Click, AddressOf myButtonManager_Click
        AddHandler myUserButtons.Click, AddressOf myUserButtons_Click
        AddHandler myDocuments.Click, AddressOf myDocuments_Click
        AddHandler myPictures.Click, AddressOf myPictures_Click
        AddHandler myVideos.Click, AddressOf myVideos_Click
        AddHandler myMusic.Click, AddressOf myMusic_Click
        AddHandler myShowTrayButton.Click, AddressOf myShowTrayButton_Click
        AddHandler myHideTrayButton.Click, AddressOf myHideTrayButton_Click
        AddHandler myOSK.Click, AddressOf myOSK_Click
        AddHandler mySpeech.Click, AddressOf mySpeech_Click
        AddHandler myHelp.Click, AddressOf myHelp_Click
        AddHandler myForm.FormClosing, AddressOf myForm_Closing
        AddHandler myRun.Click, AddressOf myRun_Click
        AddHandler myAlertListButton.Click, AddressOf myAlertListButton_Click
        AddHandler myForm.MouseWheel, AddressOf myform_MouseScroll
        AddHandler myProgsUp.Click, AddressOf ProgBack
        AddHandler myProgsBack.Click, AddressOf previousProgPage
        AddHandler myProgsNext.Click, AddressOf nextProgPage

        setDoubleBuffered(myClock)

        MyPrograms = GetAllPrograms
        loadProgList()

        'Create arrows for window list
        leftArrow = New LCARS.Controls.ArrowButton()
        leftArrow.ArrowDirection = LCARS.LCARSarrowDirection.Left
        leftArrow.Size = New Point(25, 25)
        leftArrow.Location = New Point(0, 0)
        leftArrow.Lit = False
        leftArrow.Name = "leftArrow"
        rightArrow = New LCARS.Controls.ArrowButton()
        AddHandler leftArrow.Click, AddressOf leftArrow_Click
        myAppsPanel.Controls.Add(leftArrow)
        rightArrow.ArrowDirection = LCARS.LCARSarrowDirection.Right
        rightArrow.Size = leftArrow.Size
        rightArrow.Anchor = AnchorStyles.Right
        rightArrow.Lit = False
        rightArrow.Name = "rightArrow"
        rightArrow.Location = New Point(myAppsPanel.Width - rightArrow.Width, 0)
        AddHandler rightArrow.Click, AddressOf rightArrow_Click
        myAppsPanel.Controls.Add(rightArrow)


        myUserButtonCollection.Clear()
        loadUserButtons()
        loadLanguage()

        LCARS.SetBeeping(myForm, modSettings.ButtonBeep)
        RegisterAlertForm(myForm)

        AddHandler mainTimer.Tick, AddressOf mainTimer_Tick


        'load Mainscreen Settings:
        tmrAutohide.Interval = 100
        Dim AutoHideMode As Integer = modSettings.AutoHide(ScreenIndex)
        SetAutoHide(AutoHideMode)
        If modSettings.ShowTrayIcons(ScreenIndex) = True Then
            myShowTrayButton_Click(New Object, New EventArgs)
        End If

        oldArea = Screen.AllScreens(ScreenIndex).WorkingArea
    End Sub

    Public Sub loadLanguage()
        Try
            Dim strinput As String = ""
            Dim split() As String
            Dim filename As String = modSettings.LanguageFileName(ScreenIndex)

            FileOpen(1, Application.StartupPath() & "\lang\" & filename, OpenMode.Input)
            Input(1, strinput)
            If strinput.ToLower = "lcars x32 language file" Then
                Do Until EOF(1)
                    Input(1, strinput)
                    If strinput.Contains("=") Then
                        split = strinput.Split("=")
                        'Makes sure that one bad line doesn't stop loading the whole language file.
                        If myForm.Controls.Find(split(0).Trim, True).Length > 0 Then
                            CType(myForm.Controls.Find(split(0).Trim, True)(0), LCARS.LCARSbuttonClass).ButtonText = split(1).Trim.Replace(Chr(34), "")
                        End If
                    End If
                Loop
            Else
                MsgBox("The file '" & filename & "' is not a valid LCARS x32 language file." _
                       & vbNewLine & vbNewLine & "LCARS x32 will use the default button text instead.")
            End If
            FileClose(1)

        Catch ex As Exception
            MsgBox("error" & vbNewLine & ex.ToString())
            FileClose(1)
        End Try

    End Sub

    Private Sub mainTimer_Tick(ByVal sender As Object, ByVal e As System.EventArgs)
        If myForm.IsDisposed Then Return ' Don't access a disposed screen.

        Dim battInfo As PowerStatus = SystemInformation.PowerStatus
        Static battLevel As Short = 10


        'Set the clock
        '-------------------------
        'Get the time and date format
        Dim newText As String = ""
        If (GetSetting("LCARS x32", "Application", "Stardate", "TRUE") <> "TRUE") Then
            Try
                Dim myReg As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser
                myReg = myReg.OpenSubKey("Control Panel\International")
                timeFormat = myReg.GetValue("sTimeFormat", "h:mm:sstt")
                dateFormat = myReg.GetValue("sShortDate", "M/d/yyyy")
            Catch ex As Exception
                timeFormat = "h:mm:sstt"
                dateFormat = "M/d/yyyy"
            End Try

            newText = Format(Now, timeFormat) & " " & Format(Now.Date, dateFormat)

        Else
            newText = LCARS.Stardate.getStardate(Now)
        End If

        If newText <> myClock.Text Then
            myClock.Text = newText
        End If

        '-------------------------


        'if we are on battery power, update the battery's status
        '-------------------------
        myBattPercent.Text = battInfo.BatteryLifePercent * 100 & "%"

        If battInfo.PowerLineStatus = PowerLineStatus.Offline Then
            myPowerSource.Text = "AUXILIARY"
        Else
            myPowerSource.Text = "PRIMARY"
        End If

        Dim newBattLevel As Short = Math.Ceiling(battInfo.BatteryLifePercent * 10)

        If newBattLevel <> battLevel Then
            battLevel = newBattLevel
            For i As Integer = 0 To bars.Length - 1
                bars(i).Lit = i < battLevel
            Next
        End If

        Dim adjustedBounds As Rectangle
        If autohide = IAutohide.AutoHideModes.Hidden Then
            adjustedBounds = Screen.FromHandle(myForm.Handle).Bounds
        Else
            adjustedBounds = New Rectangle(myMainPanel.PointToScreen(Drawing.Point.Empty), myMainPanel.Size)
        End If
        If Not adjustedBounds = oldArea Then
            'The working area needs to change, alert the linked windows (if there are any).
            If LinkedWindows.Count > 0 Then
                Dim myRectData As New COPYDATASTRUCT
                myRectData.dwData = 100
                myRectData.cdData = Marshal.SizeOf(GetType(Rectangle))

                Dim myPtr As IntPtr = Marshal.AllocCoTaskMem(myRectData.cdData)
                Marshal.StructureToPtr(adjustedBounds, myPtr, False)

                myRectData.lpData = myPtr

                Dim MyCopyData As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(GetType(COPYDATASTRUCT)))
                Marshal.StructureToPtr(myRectData, MyCopyData, False)
                'Do not use SendDataToLinkedWindows; it uses PostMessage, not SendMessage
                For Each targetHandle As IntPtr In LinkedWindows
                    If Screen.ReferenceEquals(Screen.FromHandle(targetHandle), Screen.FromHandle(myForm.Handle)) Then
                        Dim res As Integer = SendMessage(targetHandle, WM_COPYDATA, myDesktop.Handle, MyCopyData)
                    End If
                Next
                Marshal.FreeCoTaskMem(MyCopyData)
                Marshal.FreeCoTaskMem(myPtr)
            End If
            resizeWorkingArea(adjustedBounds.X, adjustedBounds.Y, adjustedBounds.Width, adjustedBounds.Height)
            oldArea = adjustedBounds
        End If

        If Not myDesktop.curDesktop(ScreenIndex).Size = adjustedBounds.Size Then
            updateDesktopBounds(ScreenIndex, adjustedBounds)
        End If

        'Deal with resizing the tray icon panel if necessary
        If myHideTrayButton.Visible = True Then
            Dim myPlacement As New WINDOWPLACEMENT
            GetWindowPlacement(hTrayIcons, myPlacement)
            Dim myWidth As Integer = myPlacement.rcNormalPosition.Right_Renamed - myPlacement.rcNormalPosition.Left_Renamed
            If myWidth <> myWidth + myHideTrayButton.Width Then
                myAppsPanel.Width -= (myWidth + myHideTrayButton.Width) - myTrayPanel.Width
                myTrayPanel.Left -= (myWidth + myHideTrayButton.Width) - myTrayPanel.Width

                myTrayPanel.Width = myWidth + myHideTrayButton.Width
            End If
        End If



        'Refresh the taskbar if necessary
        'myWindows.Clear()

        WindowList.Clear()
        'find all the windows
        EnumWindows(New EnumCallBack(AddressOf fEnumWindowsCallBack), 0)

        Dim windowChange As Boolean = False
        'Check for new windows
        For Each curWindow As ExternalApp In WindowList
            Dim found As Boolean = False
            For Each myWindow As ExternalApp In myWindows
                If myWindow.hWnd = curWindow.hWnd Then
                    myWindow.MainWindowText = curWindow.MainWindowText
                    found = True
                    Exit For
                End If
            Next
            If Not found Then
                myWindows.Add(curWindow)
                windowChange = True
            End If
        Next
        Dim countOffset As Integer = 0
        For i As Integer = 0 To myWindows.Count() - 1
            Dim myWindow As ExternalApp = myWindows(i - countOffset)
            Dim found As Boolean = False
            For Each curWindow As ExternalApp In WindowList
                If myWindow.hWnd = curWindow.hWnd Then
                    found = True
                    Exit For
                End If
            Next
            If Not found Then
                myWindows.Remove(myWindow)
                countOffset += 1
                windowChange = True
            End If
        Next


        'refresh the taskbar
        If windowChange Then
            Dim beeping As Boolean = modSettings.ButtonBeep

            myAppsPanel.Controls.Clear()
            myAppsPanel.Controls.Add(leftArrow)

            For intloop As Integer = 0 To myWindows.Count() - 1
                Dim myButton As New LCARS.Controls.HalfPillButton
                Dim myCloseButton As New LCARS.Controls.FlatButton
                myCloseButton.Size = New Point(20, 25)
                myCloseButton.Text = "X"
                myCloseButton.ButtonTextAlign = ContentAlignment.MiddleCenter
                myCloseButton.Color = LCARS.LCARScolorStyles.FunctionOffline
                myCloseButton.Left = (((myAppsPanel.Controls.Count - 1) \ 2) * 134) + 31
                myCloseButton.Top = 0
                myCloseButton.Data = myWindows(intloop).hWnd
                myCloseButton.Beeping = beeping
                myCloseButton.Tag = (intloop + 6).ToString
                myAppsPanel.Controls.Add(myCloseButton)

                AddHandler myCloseButton.Click, AddressOf CloseButton_Click


                myButton.Text = myWindows(intloop).MainWindowText
                myButton.Size = New Point(100, 25)
                myButton.Left = (((myAppsPanel.Controls.Count - 1) \ 2) * 134) + 56
                myButton.Top = 0
                myButton.Beeping = False
                myButton.Data = myWindows(intloop).hWnd
                myButton.Beeping = beeping
                myButton.ButtonTextAlign = ContentAlignment.TopLeft
                myButton.Lit = (getWindowState(myWindows(intloop).hWnd) <> WindowStates.MINIMIZED)

                myButton.Tag = (intloop + 6).ToString

                myAppsPanel.Controls.Add(myButton)

                AddHandler myButton.Click, AddressOf AppsButton_Click
            Next
            myAppsPanel.Controls.Add(rightArrow)
            rightArrow.BringToFront()
            myAppsPanel.Tag = (myWindows.Count() + 6).ToString
        Else
            For Each curWindow As ExternalApp In myWindows
                For Each myButton As LCARS.LCARSbuttonClass In myAppsPanel.Controls
                    If CInt(myButton.Data) = curWindow.hWnd Then

                        If getWindowState(curWindow.hWnd) = WindowStates.MINIMIZED Then
                            If myButton.Lit Then
                                myButton.Lit = False
                            End If
                        Else
                            If myButton.Lit = False Then
                                myButton.Lit = True
                            End If
                        End If
                        If myButton.Color = LCARS.LCARScolorStyles.FunctionOffline Then Continue For
                        If Not myButton.ButtonText.Equals(curWindow.MainWindowText, StringComparison.CurrentCultureIgnoreCase) Then
                            myButton.ButtonText = curWindow.MainWindowText
                        End If
                    End If
                Next
            Next
        End If

        'Display topmost window
        Dim topmost As Integer = GetForegroundWindow()
        If curTop <> topmost AndAlso Not myForm.IsDisposed AndAlso topmost <> myForm.Handle.ToInt32() Then
            curTop = topmost
            For Each mybutton As LCARS.LCARSbuttonClass In myAppsPanel.Controls
                If mybutton.Color <> LCARS.LCARScolorStyles.FunctionOffline Then
                    If mybutton.Data = curTop Then
                        mybutton.Color = LCARS.LCARScolorStyles.PrimaryFunction
                    Else
                        mybutton.Color = LCARS.LCARScolorStyles.MiscFunction
                    End If
                End If
            Next
        End If
    End Sub
    'Moves the taskbar buttons to the right
    Private Sub rightArrow_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        For Each myControl As LCARS.LCARSbuttonClass In myAppsPanel.Controls
            If Not (myControl.Name.ToLower = "leftarrow" Or myControl.Name.ToLower = "rightarrow") Then
                myControl.Left -= 134
            End If
        Next
    End Sub
    'Moves the taskbar buttons to the left
    Private Sub leftArrow_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        For Each myControl As LCARS.LCARSbuttonClass In myAppsPanel.Controls
            If Not (myControl.Name.ToLower = "leftarrow" Or myControl.Name.ToLower = "rightarrow") Then
                myControl.Left += 134
            End If
        Next
    End Sub
    'Red "X" next to the taskbar button
    Private Sub CloseButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        CloseWindow(CInt(CType(sender, LCARS.LCARSbuttonClass).Data))
    End Sub
    'Taskbar button
    Private Sub AppsButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim myButton As LCARS.LCARSbuttonClass = CType(sender, LCARS.LCARSbuttonClass)
        Dim myHandle As Integer = myButton.Data

        If myButton.Color = LCARS.LCARScolorStyles.PrimaryFunction Then
            If getWindowState(myHandle) <> WindowStates.MINIMIZED Then
                myButton.Data2 = getWindowState(myHandle)
                SetWindowState(myHandle, WindowStates.MINIMIZED)
            Else
                If Not myButton.Data2 Is Nothing Then
                    SetWindowState(myHandle, myButton.Data2)
                Else
                    SetWindowState(myHandle, WindowStates.NORMAL)
                End If
            End If
        Else
            If getWindowState(myHandle) = WindowStates.MINIMIZED Then
                If Not myButton.Data2 Is Nothing Then
                    SetWindowState(myHandle, myButton.Data2)
                Else
                    SetWindowState(myHandle, WindowStates.NORMAL)
                End If
            End If
            SetTopWindow(myHandle)
        End If
    End Sub

    Public Sub loadUserButtons()

        If Not UserButtonsPanel Is Nothing Then
            Dim buttonTop As Integer = 0
            Dim myReg As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser

            UserButtonsPanel.Clear()
            myUserButtonCollection.Clear()

            myReg = myReg.OpenSubKey("Software\VB and VBA Program Settings\LCARS x32\UserButtons", False)

            If Not myReg Is Nothing Then
                For intloop As Integer = 0 To myReg.ValueCount - 1

                    Dim mybutton As New LCARS.LightweightControls.LCFlatButton
                    Dim myUserButtonInfo As New UserButtonInfo


                    mybutton.Beeping = False
                    mybutton.Color = LCARS.LCARScolorStyles.MiscFunction

                    AddHandler mybutton.Click, AddressOf myfile_click

                    If IsNumeric(myReg.GetValueNames(intloop).Substring(0, 2)) Then
                        mybutton.Text = myReg.GetValueNames(intloop).Substring(2)
                    Else
                        mybutton.Text = myReg.GetValueNames(intloop)
                    End If

                    myUserButtonInfo.Name = mybutton.Text
                    'myUserButtonInfo.color = Convert.ToInt32(myReg.GetValueNames(intloop).Substring(0, 2))
                    mybutton.Data = myReg.GetValue(myReg.GetValueNames(intloop))
                    myUserButtonInfo.Location = mybutton.Data

                    UserButtonsPanel.Add(mybutton)

                    AddUserButton(myUserButtonInfo, True)
                Next
            End If
        End If

    End Sub

    Public Sub AddUserButton(ByVal button As UserButtonInfo, Optional ByVal DontSave As Boolean = False)
        myUserButtonCollection.Add(button)
        If DontSave = False Then
            SaveUserButtons()
            loadUserButtons()
        End If

    End Sub

    Public Sub RemoveUserButton(ByVal index As Integer)
        myUserButtonCollection.RemoveAt(index)
        SaveUserButtons()
        loadUserButtons()
    End Sub

    Public Sub EditUserButton(ByVal button As UserButtonInfo, ByVal index As Integer)
        myUserButtonCollection(index) = button
        SaveUserButtons()
        loadUserButtons()
    End Sub

    Public Sub SaveUserButtons()
        Try
            'remove current userbuttons
            Dim myReg As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser
            myReg = myReg.OpenSubKey("Software\VB and VBA Program Settings\LCARS x32\UserButtons", True)
            Dim myValues() As String = myReg.GetValueNames()

            For Each myValue As String In myValues
                myReg.DeleteValue(myValue)
            Next
        Catch ex As Exception

        End Try

        Dim intCount As Integer = 0
        Dim index As Integer
        For index = 0 To myUserButtonCollection.Count - 1
            Dim myobject As Object = myUserButtonCollection(index)
            Dim myButton As UserButtonInfo
            myButton = CType(myobject, UserButtonInfo)
            Try
                SaveSetting("LCARS x32", "UserButtons", intCount.ToString("D2") & myButton.Name, myButton.Location)
            Catch ex As Exception
                'MsgBox(ex.ToString())
            End Try
            intCount += 1
        Next
    End Sub

#Region " Start Menu Handlers "
    Private Sub ProgramsPanel_Resize(ByVal sender As Object, ByVal e As System.EventArgs)
        If myForm.WindowState <> FormWindowState.Minimized Then
            loadProgList(curProgIndex)
        End If
    End Sub

    Private Sub loadProgList(Optional ByVal index As Integer = 0)
        Dim itemCount As Integer = 0
        Dim myDir As DirectoryStartItem
        Dim pageMax As Integer

        ProgPageSize = ProgramsPanel.Height \ 30
        index = index - (index Mod ProgPageSize)
        curProgIndex = index

        myDir = MyPrograms
        For Each myindex As Integer In ProgDir
            myDir = myDir.subItems(myindex)
        Next
        ProgramsPanel.Clear()

        pageCount = Int(myDir.subItems.Count / ProgPageSize)

        If myDir.subItems.Count Mod ProgPageSize > 0 Then
            pageCount += 1
        End If

        curProgPage = (index \ ProgPageSize) + 1

        myProgramPagesDisplay.Text = "PAGES " & curProgPage & " of " & pageCount


        pageMax = ProgPageSize + (index - 1)

        If pageMax > myDir.subItems.Count - 1 Then
            pageMax = myDir.subItems.Count - 1
        End If

        For intloop As Integer = index To pageMax
            If myDir.subItems(intloop).GetType Is GetType(DirectoryStartItem) Then
                With CType(myDir.subItems(intloop), DirectoryStartItem)
                    Dim myButton As New LCARS.LightweightControls.LCComplexButton
                    myButton.HoldDraw = True
                    myButton.Width = ProgramsPanel.Width
                    myButton.Height = 25
                    myButton.Left = 0
                    myButton.Color = LCARS.LCARScolorStyles.NavigationFunction
                    myButton.Text = .Name
                    myButton.SideText = .subItems.Count
                    myButton.TextHeight = 14
                    myButton.TextAlign = ContentAlignment.BottomRight
                    myButton.Data = intloop
                    myButton.Top = itemCount * 30
                    myButton.Beeping = False
                    myButton.Data2 = ((pageMax - index) - (intloop - index)).ToString
                    ProgramsPanel.Add(myButton)
                    myButton.HoldDraw = False
                    AddHandler myButton.Click, AddressOf myDir_click
                    itemCount += 1
                End With
            Else
                With CType(myDir.subItems(intloop), FileStartItem)
                    Dim myButton As New LCARS.LightweightControls.LCStandardButton
                    myButton.HoldDraw = True
                    myButton.Width = ProgramsPanel.Width
                    myButton.Height = 25
                    myButton.Left = 0
                    myButton.Color = LCARS.LCARScolorStyles.MiscFunction
                    myButton.Text = Path.GetFileNameWithoutExtension(.Name)
                    myButton.Data = .Link.Executable
                    myButton.Top = itemCount * 30
                    myButton.Beeping = False
                    myButton.Data2 = ((pageMax - index) - (intloop - index)).ToString
                    ProgramsPanel.Add(myButton)
                    myButton.HoldDraw = False
                    AddHandler myButton.Click, AddressOf startItem_click
                    itemCount += 1
                End With
            End If
        Next
        ProgramsPanel.Tag = (pageMax - index).ToString

        'Update buttons
        myProgsNext.Lit = curProgPage < pageCount
        myProgsBack.Lit = curProgPage > 1
        myProgsUp.Lit = ProgDir.Count > 0

    End Sub

    Public Sub nextProgPage()
        If curProgPage < pageCount Then
            curProgPage += 1
            loadProgList((curProgPage * ProgPageSize) - (ProgPageSize - 1))
        End If
    End Sub

    Public Sub previousProgPage()
        If curProgPage > 1 Then
            curProgPage -= 1
            loadProgList((curProgPage * ProgPageSize) - (ProgPageSize - 1))
        End If
    End Sub

    Public Sub ProgBack()
        If ProgDir.Count > 0 Then
            Dim index As Integer = ProgDir(ProgDir.Count - 1)
            ProgDir.RemoveAt(ProgDir.Count - 1)
            loadProgList(index)
        End If
    End Sub

    Private Sub myDir_click(ByVal sender As Object, ByVal e As System.EventArgs)
        ProgDir.Add(sender.data)
        loadProgList()
    End Sub

    Private Sub startItem_click(ByVal sender As Object, ByVal e As System.EventArgs)
        If ProgramsPanel.Visible Then myStartMenu.doClick(sender, e)
        Application.DoEvents()
        Dim myprocess As New System.Diagnostics.Process()
        myprocess.StartInfo.FileName = CType(sender, LCARS.LightweightControls.LCFlatButton).Data
        launchProcessOnScreen(myprocess)
    End Sub
#End Region

    'Used for buttons in Personal Programs (userbuttons)
    Private Sub myfile_click(ByVal sender As Object, ByVal e As System.EventArgs)
        If UserButtonsPanel.Visible Then
            myUserButtons.doClick(sender, e)
        End If
        Application.DoEvents()
        Dim cmdLine As String = CType(CType(sender, LCARS.LightweightControls.LCFlatButton).Data, String).Trim()
        Dim myprocess As New Process()
        If File.Exists(cmdLine) Then
            'The command string is an absolute path.
            myprocess.StartInfo.FileName = sender.data
            myprocess.StartInfo.WorkingDirectory = Path.GetDirectoryName(sender.data)
            launchProcessOnScreen(myprocess)
        Else
            'The command will be interpreted as a command followed by arguments
            Try
                If (sender.data.Substring(0, 1) = """") Then
                    Dim splitIndex As Integer = sender.data.Substring(1).IndexOf("""") + 2
                    myprocess.StartInfo.FileName = sender.data.Substring(0, splitIndex)
                    myprocess.StartInfo.Arguments = sender.data.Substring(splitIndex + 1)
                Else
                    myprocess.StartInfo.FileName = sender.data.Split(" ")(0)
                    myprocess.StartInfo.Arguments = sender.data.Substring(myprocess.StartInfo.FileName.Length + 1)
                End If
                'If full path specified, set working directory to containing folder
                If File.Exists(myprocess.StartInfo.FileName) Then
                    myprocess.StartInfo.WorkingDirectory = Path.GetDirectoryName(myprocess.StartInfo.FileName)
                End If
                launchProcessOnScreen(myProcess)
            Catch ex As Exception
                Debug.Print("Failed to interpret command line")
                'Throw it to shell and see what happens.
                Try
                    Dim myID As Integer
                    myID = Shell(sender.data, AppWinStyle.NormalFocus)
                    myprocess = Process.GetProcessById(myID)
                    launchProcessOnScreen(myprocess, False)
                Catch ex2 As ArgumentException
                    'Process already exited before we could get it.
                Catch ex2 As FileNotFoundException
                    MsgBox("Bad command string: " & cmdLine)
                End Try
            End Try
        End If
    End Sub

    Public Sub launchProcessOnScreen(ByVal myProcess As Process, Optional ByVal needsStart As Boolean = True)
        If needsStart Then
            Try
                If Not myProcess.Start() Then Return
            Catch ex As System.ComponentModel.Win32Exception
                MsgBox("Unable to start process")
                Return
            End Try
        End If
        Dim sw As New Stopwatch()
        sw.Start()
        Do Until myProcess.HasExited OrElse _
                 myProcess.MainWindowHandle <> IntPtr.Zero OrElse _
                 sw.ElapsedMilliseconds > 15000L
            myProcess.Refresh()
            Application.DoEvents()
            Threading.Thread.Sleep(50)
        Loop
        sw.Stop()
        If Not myProcess.HasExited AndAlso _
               myProcess.MainWindowHandle <> IntPtr.Zero Then
            MoveToScreen(myProcess.MainWindowHandle)
        End If
    End Sub


    Private Sub MoveToScreen(ByVal hWnd As IntPtr)
        If MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST) _
                = MonitorFromWindow(myForm.Handle, MONITOR_DEFAULTTONEAREST) Then
            'Already on correct screen
            Return
        End If
        Dim myScreen As Screen = Screen.FromHandle(myForm.Handle)
        Dim myPlacement As New WINDOWPLACEMENT
        Dim isMax As Boolean = False

        myPlacement.Length = Marshal.SizeOf(myPlacement)

        GetWindowPlacement(hWnd, myPlacement)

        myPlacement.ptMaxPosition.X = myForm.Left
        myPlacement.ptMaxPosition.Y = myForm.Top

        myPlacement.rcNormalPosition.Right_Renamed -= myPlacement.rcNormalPosition.Left_Renamed - myForm.Left
        myPlacement.rcNormalPosition.Bottom_Renamed -= myPlacement.rcNormalPosition.Top_Renamed - myForm.Top
        myPlacement.rcNormalPosition.Left_Renamed = myForm.Location.X
        myPlacement.rcNormalPosition.Top_Renamed = myForm.Location.Y

        If myPlacement.ShowCmd = WindowStates.MAXIMIZED Then
            isMax = True
            myPlacement.ShowCmd = WindowStates.NORMAL
        End If

        SetWindowPlacement(hWnd, myPlacement)

        If isMax Then
            myPlacement.ShowCmd = WindowStates.MAXIMIZED
            SetWindowPlacement(hWnd, myPlacement)
        End If
    End Sub

    Public Sub myForm_Closing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs)
        e.Cancel = True
        myDeactivate.doClick(sender, e)
    End Sub

    Private Function FindRoot(ByVal hWnd As Int32) As Int32
        Do
            Dim parent_hwnd As Int32 = GetParent(hWnd)
            If parent_hwnd = 0 Then Return hWnd
            hWnd = parent_hwnd
        Loop
    End Function

    Public Sub SetAutoHide(ByVal value As IAutohide.AutoHideModes)

        autohide = value
        If autohide = IAutohide.AutoHideModes.Disabled Then
            tmrAutohide.Enabled = False
            hideCount = 0
            myForm.Visible = True
        Else
            autohide = IAutohide.AutoHideModes.Visible
            tmrAutohide.Enabled = True
        End If
    End Sub

    Private Sub tmrAutoHide_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tmrAutohide.Tick
        If Not autohide = IAutohide.AutoHideModes.Disabled Then
            Dim myPoint As POINTAPI
            myPoint.X = Cursor.Position.X
            myPoint.Y = Cursor.Position.Y

            Dim rootHwnd As IntPtr = FindRoot(WindowFromPoint(myPoint))

            'The mouse must be within this many pixels of the edge to show the screen
            Const edgeWidth As Integer = 1

            Dim edges As IAutohide.AutohideEdges = CType(myForm, IAutohide).getAutohideEdges()
            Dim isAtEdge As Boolean = False

            If myForm.Bounds.Contains(myPoint.X, myPoint.Y) Then
                If (edges And IAutohide.AutohideEdges.Top) = IAutohide.AutohideEdges.Top Then
                    isAtEdge = isAtEdge Or (myPoint.Y < myForm.Top + edgeWidth And myPoint.Y >= myForm.Top)
                End If
                If (edges And IAutohide.AutohideEdges.Left) = IAutohide.AutohideEdges.Left Then
                    isAtEdge = isAtEdge Or (myPoint.X < myForm.Left + edgeWidth And myPoint.X >= myForm.Left)
                End If
                If (edges And IAutohide.AutohideEdges.Bottom) = IAutohide.AutohideEdges.Bottom Then
                    isAtEdge = isAtEdge Or (myPoint.Y >= myForm.Bottom - edgeWidth And myPoint.Y <= myForm.Bottom)
                End If
                If (edges And IAutohide.AutohideEdges.Right) = IAutohide.AutohideEdges.Right Then
                    isAtEdge = isAtEdge Or (myPoint.X >= myForm.Right - edgeWidth And myPoint.X <= myForm.Right)
                End If
            End If
            If rootHwnd = myForm.Handle Or isAtEdge Or _
                    progShowing = True Or userButtonsShowing = True Then
                hideCount = 0

                If Not autohide = IAutohide.AutoHideModes.Visible Or myForm.Visible = False Then
                    myForm.Visible = True
                    autohide = IAutohide.AutoHideModes.Visible
                End If
            End If

            If hideCount <= 30 Then
                hideCount += 1
            Else
                autohide = IAutohide.AutoHideModes.Hidden
                myForm.Visible = False
            End If
        Else
            tmrAutohide.Enabled = False
        End If

    End Sub

    Public Sub resetWorkingArea()
        oldArea = New Rectangle(1, 1, 1, 1)
    End Sub

    Public ReadOnly Property WorkingArea() As Rectangle
        Get
            Return oldArea
        End Get
    End Property

    Public Sub myform_MouseScroll(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        If Not ProgramsPanel.Visible Then Return
        If e.Delta > 0 Then
            previousProgPage()
        Else
            nextProgPage()
        End If
    End Sub

    Public Sub UpdateRegion()
        Dim myRegion As Region = New Region(New RectangleF(0, 0, myForm.Width, myForm.Height))
        Dim mainRect As New Rectangle(myForm.PointToClient(myMainPanel.PointToScreen(Drawing.Point.Empty)), myMainPanel.Size)
        myRegion.Exclude(mainRect)
        myForm.Region = myRegion
    End Sub
End Class
