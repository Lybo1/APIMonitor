#pragma once

#include <QOpenGLWidget>
#include <QOpenGLFunctions>
#include <QTimer>

class globeopenglwidget : public QOpenGLWidget, protected QOpenGLFunctions {
    Q_OBJECT
public:
    explicit globeopenglwidget(QWidget *parent = nullptr);
    ~globeopenglwidget();

protected:
    void initializeGL() override;
    void resizeGL(int w, int h) override;
    void paintGL() override;

private:
    float rotation = 0.0f;
    QTimer *timer;
};
