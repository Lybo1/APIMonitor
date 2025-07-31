#include "mainwindow.h"
#include "./ui_mainwindow.h"
#include "globeopenglwidget.h"

#include <QVBoxLayout>  // <-- Add this!

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent), ui(new Ui::MainWindow) {
    ui->setupUi(this);

    QWidget *central = new QWidget(this);
    QVBoxLayout *layout = new QVBoxLayout(central);

    // Match your class name EXACTLY here:
    layout->addWidget(new globeopenglwidget(this));

    central->setLayout(layout);
    setCentralWidget(central);
}

MainWindow::~MainWindow() {
    delete ui;
}
