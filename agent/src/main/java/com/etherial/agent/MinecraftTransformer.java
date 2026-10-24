package com.etherial.agent;

import org.objectweb.asm.*;
import java.lang.instrument.ClassFileTransformer;
import java.security.ProtectionDomain;

public class MinecraftTransformer implements ClassFileTransformer {

    @Override
    public byte[] transform(
            ClassLoader loader,
            String className,
            Class<?> classBeingRedefined,
            ProtectionDomain protectionDomain,
            byte[] classfileBuffer
    ) {
        if (className == null) {
            return classfileBuffer;
        }

        // 1. Hook Minecraft Main Class (Title + Main Menu Blocker)
        if (className.equals("net/minecraft/client/Minecraft") || className.equals("ave")) {
            System.out.println("[Etherial Agent] Transforming Minecraft class: " + className);
            try {
                ClassReader cr = new ClassReader(classfileBuffer);
                ClassWriter cw = new ClassWriter(cr, ClassWriter.COMPUTE_FRAMES | ClassWriter.COMPUTE_MAXS);

                ClassVisitor cv = new ClassVisitor(Opcodes.ASM9, cw) {
                    @Override
                    public MethodVisitor visitMethod(
                            int access,
                            String name,
                            String descriptor,
                            String signature,
                            String[] exceptions
                    ) {
                        MethodVisitor mv = super.visitMethod(access, name, descriptor, signature, exceptions);

                        return new MethodVisitor(Opcodes.ASM9, mv) {
                            @Override
                            public void visitCode() {
                                super.visitCode();

                                boolean isDisplayGui = (name.equals("displayGuiScreen") && descriptor.contains("GuiScreen"))
                                        || (name.equals("a") && descriptor.equals("(Laxu;)V"));

                                if (isDisplayGui) {
                                    super.visitVarInsn(Opcodes.ALOAD, 0); // this (Minecraft)
                                    super.visitVarInsn(Opcodes.ALOAD, 1); // targetScreen (GuiScreen)
                                    super.visitMethodInsn(
                                            Opcodes.INVOKESTATIC,
                                            "com/etherial/agent/AutoConnectBridge",
                                            "replaceScreen",
                                            "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;",
                                            false
                                    );
                                    String screenType = descriptor.substring(2, descriptor.length() - 3);
                                    super.visitTypeInsn(Opcodes.CHECKCAST, screenType);
                                    super.visitVarInsn(Opcodes.ASTORE, 1);
                                }
                            }

                            @Override
                            public void visitLdcInsn(Object value) {
                                if ("Minecraft 1.8.9".equals(value)) {
                                    super.visitLdcInsn("Etherial");
                                    return;
                                }
                                super.visitLdcInsn(value);
                            }

                            @Override
                            public void visitMethodInsn(int opcode, String owner, String name, String descriptor, boolean isInterface) {
                                if (owner.equals("org/lwjgl/opengl/Display") && name.equals("setTitle")) {
                                    super.visitInsn(Opcodes.POP);
                                    super.visitLdcInsn("Etherial");
                                }
                                super.visitMethodInsn(opcode, owner, name, descriptor, isInterface);
                            }
                        };
                    }
                };

                cr.accept(cv, ClassReader.EXPAND_FRAMES);
                return cw.toByteArray();
            } catch (Throwable t) {
                System.err.println("[Etherial Agent] Failed to transform " + className + ": " + t.getMessage());
            }
        }

        // 2. Hook Ingame Pause Menu (Direct Quit on Disconnect)
        if (className.equals("net/minecraft/client/gui/GuiIngameMenu") || className.equals("axp")) {
            System.out.println("[Etherial Agent] Transforming GuiIngameMenu class: " + className);
            try {
                ClassReader cr = new ClassReader(classfileBuffer);
                ClassWriter cw = new ClassWriter(cr, ClassWriter.COMPUTE_FRAMES | ClassWriter.COMPUTE_MAXS);

                ClassVisitor cv = new ClassVisitor(Opcodes.ASM9, cw) {
                    @Override
                    public MethodVisitor visitMethod(int access, String name, String descriptor, String signature, String[] exceptions) {
                        MethodVisitor mv = super.visitMethod(access, name, descriptor, signature, exceptions);

                        return new MethodVisitor(Opcodes.ASM9, mv) {
                            @Override
                            public void visitCode() {
                                super.visitCode();

                                boolean isActionPerformed = (name.equals("actionPerformed") && descriptor.contains("GuiButton"))
                                        || (name.equals("a") && descriptor.equals("(Lavs;)V"));

                                if (isActionPerformed) {
                                    Label continueLabel = new Label();
                                    super.visitVarInsn(Opcodes.ALOAD, 1); // button
                                    super.visitMethodInsn(
                                            Opcodes.INVOKESTATIC,
                                            "com/etherial/agent/AutoConnectBridge",
                                            "handleIngameMenuClick",
                                            "(Ljava/lang/Object;)Z",
                                            false
                                    );
                                    super.visitJumpInsn(Opcodes.IFEQ, continueLabel);
                                    super.visitInsn(Opcodes.RETURN);
                                    super.visitLabel(continueLabel);
                                }
                            }

                            @Override
                            public void visitLdcInsn(Object value) {
                                if ("menu.disconnect".equals(value) || "menu.returnToMenu".equals(value)) {
                                    super.visitLdcInsn("Oyundan Çık");
                                    return;
                                }
                                super.visitLdcInsn(value);
                            }
                        };
                    }
                };

                cr.accept(cv, ClassReader.EXPAND_FRAMES);
                return cw.toByteArray();
            } catch (Throwable t) {
                System.err.println("[Etherial Agent] Failed to transform " + className + ": " + t.getMessage());
            }
        }

        // 3. Hook Yggdrasil Session Service (In-game Player Skin Restoration)
        if (className.equals("com/mojang/authlib/yggdrasil/YggdrasilMinecraftSessionService")) {
            System.out.println("[Etherial Agent] Transforming YggdrasilMinecraftSessionService class: " + className);
            try {
                ClassReader cr = new ClassReader(classfileBuffer);
                ClassWriter cw = new ClassWriter(cr, ClassWriter.COMPUTE_FRAMES | ClassWriter.COMPUTE_MAXS);

                ClassVisitor cv = new ClassVisitor(Opcodes.ASM9, cw) {
                    @Override
                    public MethodVisitor visitMethod(int access, String name, String descriptor, String signature, String[] exceptions) {
                        MethodVisitor mv = super.visitMethod(access, name, descriptor, signature, exceptions);

                        if (name.equals("getTextures") && descriptor.contains("GameProfile")) {
                            return new MethodVisitor(Opcodes.ASM9, mv) {
                                @Override
                                public void visitCode() {
                                    super.visitCode();
                                    super.visitVarInsn(Opcodes.ALOAD, 1); // GameProfile
                                    super.visitMethodInsn(
                                            Opcodes.INVOKESTATIC,
                                            "com/etherial/agent/SkinBridge",
                                            "populateSkin",
                                            "(Ljava/lang/Object;)V",
                                            false
                                    );
                                }
                            };
                        }

                        return mv;
                    }
                };

                cr.accept(cv, ClassReader.EXPAND_FRAMES);
                return cw.toByteArray();
            } catch (Throwable t) {
                System.err.println("[Etherial Agent] Failed to transform " + className + ": " + t.getMessage());
            }
        }

        return classfileBuffer;
    }
}
