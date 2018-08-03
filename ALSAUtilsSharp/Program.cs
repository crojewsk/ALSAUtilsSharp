using System;
using ALSASharp;

namespace ALSASharpUtils
{
    class MainClass
    {
        public static void DeviceList()
        {
            SoundPcmStreamType stream = SoundPcmStreamType.Playback;
            SoundCardCollection enumerator = new SoundCardCollection();
            int card = -1;
            int dev;
            if (enumerator.Next(out card) < 0 || card < 0)
            {
                Console.WriteLine("no soundcards found...");
                return;
            }

            Console.WriteLine("**** List of {0} Hardware Devices ****", stream.GetName());
            int err;
            SoundControl ctl = new SoundControl();
            SoundControlCardInfo info = new SoundControlCardInfo();
            SoundPcmInfo pcminfo = new SoundPcmInfo();
            err = info.Allocate();
            err = pcminfo.Allocate();

            while (card >= 0)
            {
                string name = string.Format("hw:{0}", card);
                err = ctl.Open(name, 0);
                if (err < 0)
                {
                    Console.WriteLine("Control open ({0}): {1}",
                                      card, SoundExtensionMethods.GetErrorMessage((err)));
                    goto next_card;
                }

                err = info.Get(ctl);
                if (err < 0)
                {
                    Console.WriteLine("Control hardware info ({0}): {1}",
                                      card, SoundExtensionMethods.GetErrorMessage((err)));
                    ctl.Close();
                    goto next_card;
                }

                dev = -1;
                while (true)
                {
                    uint count;
                    SoundControlPcmCollection pcms = new SoundControlPcmCollection();
                    if (pcms.Next(ctl, out dev) < 0)
                    {
                        Console.WriteLine("snd_ctl_pcm_next_device");
                    }

                    if (dev < 0)
                        break;

                    pcminfo.SetDevice((uint)dev);
                    pcminfo.SetSubdevice(0);
                    pcminfo.SetStream(stream);
                    err = pcminfo.Get(ctl);
                    if (err < 0)
                    {
                        if (err != -2) // ENOENT
                        {
                            Console.WriteLine("error control digital audio info ({0}): {1}",
                                              card, SoundExtensionMethods.GetErrorMessage(err));
                        }
                        continue;
                    }

                    Console.WriteLine("card {0}: {1} [{2}], device {3}: {4} [{5}]",
                                      card, info.GetId(), info.GetName(),
                                      dev,
                                      pcminfo.GetId(),
                                      pcminfo.GetName());
                    count = pcminfo.GetSubdevicesCount();
                    Console.WriteLine("  Subdevices: {0}/{1}", pcminfo.GetSubdevicesAvail(), count);

                    for (uint idx = 0; idx < count; idx++)
                    {
                        pcminfo.SetSubdevice(idx);
                        err = pcminfo.Get(ctl);
                        if (err < 0)
                        {
                            Console.WriteLine("control digital audio playback info ({0}): {1}",
                                              card, SoundExtensionMethods.GetErrorMessage(err));
                        }
                        else
                        {
                            Console.WriteLine("  Subdevice #{0}: {1}", idx, pcminfo.GetSubdeviceName());
                        }
                    }
                }

                ctl.Close();

            next_card:
                if (enumerator.Next(out card) < 0)
                {
                    Console.WriteLine("error snd_card_next");
                    break;
                }
            }

            info.Deallocate();
            pcminfo.Deallocate();
            info = null;
            pcminfo = null;
        }

        public static void PcmList(SoundPcmStreamType streamType)
        {
            string name, descr, descri, io;
            string filter;
            string[] n;
            SoundDeviceNameHint nameHint = new SoundDeviceNameHint(-1, "pcm");

            if (nameHint.Hints == null)
                return;

            n = nameHint.Hints;
            filter = streamType == SoundPcmStreamType.Playback ? "Output" : "Input";

            foreach (string hint in n)
            {
                name = SoundDeviceNameHint.GetHint(hint, DeviceNameHints.NAME);
                descr = SoundDeviceNameHint.GetHint(hint, DeviceNameHints.DESC);
                io = SoundDeviceNameHint.GetHint(hint, DeviceNameHints.IOID);
                if (io != null && (string.Compare(io, filter) != 0))
                {
                    goto __end;
                }

                Console.WriteLine("{0}", name);
                descri = descr;

                if (descri != null)
                {
                    Console.WriteLine("    " + descri.Replace("\n", "\n    "));
                }

            __end:
                continue;
            }

            nameHint.Dispose();
        }

        public static void Main(string[] args)
        {
            DeviceList();
            PcmList(SoundPcmStreamType.Playback);
            PcmList(SoundPcmStreamType.Capture);
        }
    }
}
