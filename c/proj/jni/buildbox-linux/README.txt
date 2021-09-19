A containerized build system for the Linux JNI binaries (ilnux-amd64.lib, linux-x86.lib)

To avoid forward compatibility issues (binaries built on newer Linux
won't run on older Linux), uses https://github.com/phusion/holy-build-box
to build against the oldest possible GLIBC version.


(See SalesForce Ticket 29286)
Novanta Inc. Case # 00029286: RE: Centos6.10 i686 + Java API [ ref:_00Di0irxY._5001Y1ViEYn:re [ ref:_00Di0irxY._5001Y1ViEYn:ref ]
