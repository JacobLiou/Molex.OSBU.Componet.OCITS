# -*- coding: utf-8 -*-
import os

def msw_for_out_channel(c):
    # SW1/SW2 级联 + 模块 9/10/11/12（见 doc/1X16.png）
    if c <= 8:
        return "MSW 1,1,2;9,1,{};".format(c)
    if c <= 16:
        return "MSW 1,1,1;10,1,{};".format(c - 8)
    if c <= 24:
        return "MSW 2,1,2;11,1,{};".format(c - 16)
    return "MSW 2,1,1;12,1,{};".format(c - 24)

def build_block(pm, channel):
    return "[{}::{}:32]\n{}\n".format(pm, channel, msw_for_out_channel(channel))

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
out_path = os.path.join(root, "switch", "interleaverSwitch-MPLUS-OUT")
ex_path = os.path.join(root, "doc", "switch", "ITL_MPLUS_SW_OUT.example")

blocks = []
for pm in range(1, 5):
    for c in range(1, 33):
        blocks.append(build_block(pm, c).rstrip())

with open(out_path, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n\n".join(blocks) + "\n")

ex_blocks = [build_block(1, c).rstrip() for c in range(1, 33)]

with open(ex_path, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n\n".join(ex_blocks) + "\n")

print("Wrote", out_path)
print("Wrote", ex_path)
print("Sample [1::1:32]:", msw_for_out_channel(1))
print("Sample [1::9:32]:", msw_for_out_channel(9))
print("Sample [1::17:32]:", msw_for_out_channel(17))
print("Sample [1::25:32]:", msw_for_out_channel(25))
print("Sample [2::2:32]:", msw_for_out_channel(2))
