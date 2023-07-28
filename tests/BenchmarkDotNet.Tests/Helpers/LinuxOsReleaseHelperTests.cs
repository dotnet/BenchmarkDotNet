using System;
using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.Tests.Helpers
{
    public class LinuxOsReleaseHelperTests
    {
        [Theory]
        [InlineData("""
                    NAME="Ubuntu"
                    VERSION="20.04.1 LTS (Focal Fossa)"
                    ID=ubuntu
                    ID_LIKE=debian
                    PRETTY_NAME="Ubuntu 20.04.1 LTS"
                    VERSION_ID="20.04"
                    HOME_URL="https://www.ubuntu.com/"
                    SUPPORT_URL="https://help.ubuntu.com/"
                    BUG_REPORT_URL="https://bugs.launchpad.net/ubuntu/"
                    PRIVACY_POLICY_URL="https://www.ubuntu.com/legal/terms-and-policies/privacy-policy"
                    VERSION_CODENAME=focal
                    UBUNTU_CODENAME=focal
                    """, "Ubuntu 20.04.1 LTS (Focal Fossa)")]
        [InlineData("""
                    PRETTY_NAME="Debian GNU/Linux 10 (buster)"
                    NAME="Debian GNU/Linux"
                    VERSION_ID="10"
                    VERSION="10 (buster)"
                    VERSION_CODENAME=buster
                    ID=debian
                    HOME_URL="https://www.debian.org/"
                    SUPPORT_URL="https://www.debian.org/support"
                    BUG_REPORT_URL="https://bugs.debian.org/"
                    """, "Debian GNU/Linux 10 (buster)")]
        [InlineData("""
                    NAME=Fedora
                    VERSION="32 (Thirty Two)"
                    ID=fedora
                    VERSION_ID=32
                    VERSION_CODENAME=""
                    PLATFORM_ID="platform:f32"
                    PRETTY_NAME="Fedora 32 (Thirty Two)"
                    ANSI_COLOR="0;34"
                    LOGO=fedora-logo-icon
                    CPE_NAME="cpe:/o:fedoraproject:fedora:32"
                    HOME_URL="https://fedoraproject.org/"
                    DOCUMENTATION_URL="https://docs.fedoraproject.org/en-US/fedora/f32/system-administrators-guide/"
                    SUPPORT_URL="https://fedoraproject.org/wiki/Communicating_and_getting_help"
                    BUG_REPORT_URL="https://bugzilla.redhat.com/"
                    REDHAT_BUGZILLA_PRODUCT="Fedora"
                    REDHAT_BUGZILLA_PRODUCT_VERSION=32
                    REDHAT_SUPPORT_PRODUCT="Fedora"
                    REDHAT_SUPPORT_PRODUCT_VERSION=32
                    PRIVACY_POLICY_URL="https://fedoraproject.org/wiki/Legal:PrivacyPolicy"
                    """, "Fedora 32 (Thirty Two)")]
        [InlineData("""
                    NAME="CentOS Linux"
                    VERSION="8 (Core)"
                    ID="centos"
                    ID_LIKE="rhel fedora"
                    VERSION_ID="8"
                    PLATFORM_ID="platform:el8"
                    PRETTY_NAME="CentOS Linux 8 (Core)"
                    ANSI_COLOR="0;31"
                    CPE_NAME="cpe:/o:centos:centos:8"
                    HOME_URL="https://www.centos.org/"
                    BUG_REPORT_URL="https://bugs.centos.org/"

                    CENTOS_MANTISBT_PROJECT="CentOS-8"
                    CENTOS_MANTISBT_PROJECT_VERSION="8"
                    REDHAT_SUPPORT_PRODUCT="centos"
                    REDHAT_SUPPORT_PRODUCT_VERSION="8"
                    """, "CentOS Linux 8 (Core)")]
        [InlineData("""
                    NAME="Arch Linux"
                    PRETTY_NAME="Arch Linux"
                    ID=arch
                    BUILD_ID=rolling
                    ANSI_COLOR="38;2;23;147;209"
                    HOME_URL="https://archlinux.org/"
                    DOCUMENTATION_URL="https://wiki.archlinux.org/"
                    SUPPORT_URL="https://bbs.archlinux.org/"
                    BUG_REPORT_URL="https://bugs.archlinux.org/"
                    LOGO=archlinux
                    """, "Arch Linux")]
        [InlineData("""
                    NAME="openSUSE Leap"
                    VERSION="15.2"
                    ID="opensuse-leap"
                    ID_LIKE="suse opensuse"
                    VERSION_ID="15.2"
                    PRETTY_NAME="openSUSE Leap 15.2"
                    ANSI_COLOR="0;32"
                    CPE_NAME="cpe:/o:opensuse:leap:15.2"
                    BUG_REPORT_URL="https://bugs.opensuse.org"
                    HOME_URL="https://www.opensuse.org/"
                    """, "openSUSE Leap 15.2")]
        [InlineData("""
                    NAME="Manjaro Linux"
                    ID=manjaro
                    PRETTY_NAME="Manjaro Linux"
                    ANSI_COLOR="1;32"
                    HOME_URL="https://manjaro.org/"
                    SUPPORT_URL="https://manjaro.org/"
                    BUG_REPORT_URL="https://bugs.manjaro.org/"
                    LOGO=manjarolinux
                    """, "Manjaro Linux")]
        [InlineData("""
                    NAME="Linux Mint"
                    VERSION="20 (Ulyana)"
                    ID=linuxmint
                    ID_LIKE=ubuntu
                    PRETTY_NAME="Linux Mint 20"
                    VERSION_ID="20"
                    HOME_URL="https://www.linuxmint.com/"
                    SUPPORT_URL="https://forums.linuxmint.com/"
                    BUG_REPORT_URL="http://linuxmint-troubleshooting-guide.readthedocs.io/en/latest/"
                    PRIVACY_POLICY_URL="https://www.linuxmint.com/"
                    VERSION_CODENAME=ulyana
                    UBUNTU_CODENAME=focal
                    """, "Linux Mint 20 (Ulyana)")]
        [InlineData("""
                    NAME="Alpine Linux"
                    ID=alpine
                    VERSION_ID=3.12.0
                    PRETTY_NAME="Alpine Linux v3.12"
                    HOME_URL="https://alpinelinux.org/"
                    BUG_REPORT_URL="https://bugs.alpinelinux.org/"
                    """, "Alpine Linux v3.12")]
        [InlineData("""
                    NAME="Solus"
                    VERSION="4.1"
                    ID="solus"
                    PRETTY_NAME="Solus"
                    ANSI_COLOR="1;34"
                    HOME_URL="https://getsol.us"
                    SUPPORT_URL="https://getsol.us/articles/contributing/getting-involved/en/"
                    BUG_REPORT_URL="https://dev.getsol.us/"
                    """, "Solus 4.1")]
        [InlineData("""
                    NAME="Red Hat Enterprise Linux"
                    VERSION="8.2 (Ootpa)"
                    ID="rhel"
                    ID_LIKE="fedora"
                    VERSION_ID="8.2"
                    PLATFORM_ID="platform:el8"
                    PRETTY_NAME="Red Hat Enterprise Linux 8.2 (Ootpa)"
                    ANSI_COLOR="0;31"
                    CPE_NAME="cpe:/o:redhat:enterprise_linux:8.2:GA"
                    HOME_URL="https://www.redhat.com/"
                    BUG_REPORT_URL="https://bugzilla.redhat.com/"
                    REDHAT_BUGZILLA_PRODUCT="Red Hat Enterprise Linux 8"
                    REDHAT_BUGZILLA_PRODUCT_VERSION=8.2
                    REDHAT_SUPPORT_PRODUCT="Red Hat Enterprise Linux"
                    REDHAT_SUPPORT_PRODUCT_VERSION="8.2"
                    """, "Red Hat Enterprise Linux 8.2 (Ootpa)")]
        [InlineData("""
                    PRETTY_NAME="Kali GNU/Linux Rolling"
                    NAME="Kali GNU/Linux"
                    ID=kali
                    VERSION="2020.2"
                    VERSION_ID="2020.2"
                    VERSION_CODENAME="kali-rolling"
                    ID_LIKE=debian
                    ANSI_COLOR="1;31"
                    HOME_URL="https://www.kali.org/"
                    SUPPORT_URL="https://forums.kali.org/"
                    BUG_REPORT_URL="https://bugs.kali.org/"
                    """, "Kali GNU/Linux 2020.2")]
        [InlineData("""
                    NAME="elementary OS"
                    VERSION="5.1.7 Hera"
                    ID=elementary
                    ID_LIKE=ubuntu
                    PRETTY_NAME="elementary OS 5.1.7 Hera"
                    LOGO=distributor-logo
                    VERSION_ID="5.1.7"
                    HOME_URL="https://elementary.io/"
                    SUPPORT_URL="https://elementary.io/support"
                    BUG_REPORT_URL="https://github.com/elementary/os/issues/new"
                    PRIVACY_POLICY_URL="https://elementary.io/privacy-policy"
                    VERSION_CODENAME=hera
                    UBUNTU_CODENAME=bionic
                    """, "elementary OS 5.1.7 Hera")]
        [InlineData("""
                    NAME="Zorin OS"
                    VERSION="15.2"
                    ID=zorin
                    ID_LIKE=ubuntu
                    PRETTY_NAME="Zorin OS 15.2"
                    VERSION_ID="15"
                    HOME_URL="https://www.zorinos.com/"
                    SUPPORT_URL="https://www.zorinos.com/help"
                    BUG_REPORT_URL="https://www.zorinos.com/contact"
                    PRIVACY_POLICY_URL="https://www.zorinos.com/legal/privacy"
                    VERSION_CODENAME=bionic
                    UBUNTU_CODENAME=bionic
                    """, "Zorin OS 15.2")]
        public void LinuxOsReleaseHelperTest(string osReleaseContent, string expectedName)
        {
            string[] lines = osReleaseContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string actualName = LinuxOsReleaseHelper.GetNameByOsRelease(lines);
            Assert.Equal(expectedName, actualName);
        }
    }
}