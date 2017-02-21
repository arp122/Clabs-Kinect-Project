using System.Runtime.InteropServices;

static class VirtualKey
{
	[DllImport("user32.dll", EntryPoint = "keybd_event")]

	public static extern void keybd_event(
		byte bVk,		// virtual key code
		byte bScan,		// often 0
		int dwFlags,	// 0: key down; 2: key up
		int dwExtraInfo // often 0  
		);

	public const byte VK_TAB = 9;
	public const byte VK_CTRL = 17;
	public const byte VK_ALT = 18;
	public const byte VK_ESC = 27;
	public const byte VK_PAGEUP = 33;
	public const byte VK_PAGEDOWN = 34;
	public const byte VK_LEFT = 37;
	public const byte VK_UP = 38;
	public const byte VK_RIGHT = 39;
	public const byte VK_DOWN = 40;
	public const byte VK_W = 87;
	public const byte VK_F5 = 116;
	public const byte VK_ADD = 187;
	public const byte VK_MINUS = 189;


	public static void LeftArrow()
	{
		SendKey(VK_LEFT);
		//keybd_event(VK_LEFT, 0, 0, 0);
		//keybd_event(VK_LEFT, 0, 1, 0);
	}
	public static void RightArrow()
	{
		SendKey(VK_RIGHT);
	}
	public static void UpArrow()
	{
		SendKey(VK_UP);
	}
	public static void DownArrow()
	{
		SendKey(VK_DOWN);
	}

	public static void PageUp()
	{
		SendKey(VK_PAGEUP);
	}

	public static void PageDown()
	{
		SendKey(VK_PAGEDOWN);
	}

	public static void Esc()
	{
		SendKey(VK_ESC);
	}

	public static void AltTab()
	{
		SendKey2(VK_ALT, VK_TAB);
	}

	public static void CtrlTab()
	{
		SendKey2(VK_CTRL, VK_TAB);
	}

	public static void CtrlW()
	{
		SendKey2(VK_CTRL, VK_W);
	}

	public static void CtrlAdd()
	{
		SendKey2(VK_CTRL, VK_ADD);
	}

	public static void CtrlMinus()
	{
		SendKey2(VK_CTRL, VK_MINUS);
	}

	public static void F5()
	{
		SendKey(VK_F5);
	}

	public static void SendKey(byte keyCode)
	{
		keybd_event(keyCode, 0, 0, 0);
		keybd_event(keyCode, 0, 2, 0);
	}

	public static void SendKey2(byte keyCodeAid, byte keyCodeMain)
	{
		keybd_event(keyCodeAid, 0, 0, 0);
		keybd_event(keyCodeMain, 0, 0, 0);
		keybd_event(keyCodeMain, 0, 2, 0);
		keybd_event(keyCodeAid, 0, 2, 0);
	}
}